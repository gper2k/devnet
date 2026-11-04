using BitPlus.Web.Site.Data;
using BitPlus;
using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Data.CommonModel;
using BitPlus.Web.Data;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Validation;
using BitPlus.Web.Services;
using BitPlus.Web.Site.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 사용자 전용 계정/인증 Web API 컨트롤러: 로그인, 회원 가입, 메일 인증, 로그아웃, 아이디 찾기·비밀번호 재설정.
/// </summary>
public class AccountController : APIControllerBase
{
    /// <summary>가입 약관 동의 기록 시스템 속성 키 (값: 동의한 약관 키 쉼표 목록, 동의 시각 = 가입 시각)</summary>
    private const string JoinTermsAgreedKey = "JoinTermsAgreed";

    /// <summary>안내 메일(인증 메일 재발송, 아이디 찾기, 비밀번호 재설정) 같은 대상 재발송 최소 간격</summary>
    private static readonly TimeSpan MailInterval = TimeSpan.FromMinutes(1);

    /// <summary>메일 검증용 고정 템플릿 키</summary>
    private const string MailConfirmTemplate = "AccountMailConfirm";

    /// <summary>아이디 찾기 메일 고정 템플릿 키</summary>
    private const string MailFindIdTemplate = "AccountFindId";

    /// <summary>비밀번호 재설정 메일 고정 템플릿 키</summary>
    private const string MailPwChangeTemplate = "AccountPwChange";

    /// <summary>가입 약관 제목 다국어 키</summary>
    private const string TermsTitleKey = "AccountJoinTerms@Title";

    /// <summary>가입 약관 본문 다국어 키</summary>
    private const string TermsBodyKey = "AccountJoinTerms@Body";

    /// <summary>비밀번호 재설정 실패 허용 횟수 (15분 이내)</summary>
    private const int MaxResetAttempts = 5;

    /// <summary>비밀번호 재설정 잠금 시간</summary>
    private static readonly TimeSpan ResetLockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// <see cref="AccountController"/> 기본 생성자.
    /// </summary>
    public AccountController() : base() { }

    #region Sign In / Sign Out

    /// <summary>
    /// 사용자 로그인을 처리합니다. 없는 계정과 비활성 계정은 같은 "로그인 정보 불일치"로 응답하고,
    /// 잠금은 비밀번호 확인 전에 검사해 잠긴 동안 인증을 중단합니다(실패 누적 시 Identity 잠금 규칙으로 잠금).
    /// 이후 메일 검증 구성(AccountRule.MailVerify)에 따라 토큰 발급과 이동 주소를 정합니다.
    /// </summary>
    /// <param name="uid">사용자 ID 또는 이메일</param>
    /// <param name="password">비밀번호</param>
    /// <param name="returnUrl">로그인 후 돌아갈 요청 페이지</param>
    /// <returns>redirect: 이동할 주소, notice: 안내 메시지 (없으면 null)</returns>
    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromForm] string? uid, [FromForm] string? password, [FromForm] string? returnUrl)
    {
        var loginId = uid?.Trim();
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            return this.Error(WebCodes.Get(330)!);

        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByNameAsync(loginId) ?? await userManager.FindByEmailAsync(loginId);
        if (user is null || !user.Enabler) return this.Error(WebCodes.Get(311)!);

        // 잠금 적용 대상이 아닌 기존 계정도 잠금 규칙이 확실히 적용되도록 켠다
        if (!user.LockoutEnabled) await userManager.SetLockoutEnabledAsync(user, true);
        if (await userManager.IsLockedOutAsync(user)) return this.Error(WebCodes.Get(310)!);

        var hasher = this.HttpContext.GetService<IPasswordHasher<AppUser>>() ?? new PasswordHasher<AppUser>();
        var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            await userManager.AccessFailedAsync(user);
            return this.Error(WebCodes.Get(await userManager.IsLockedOutAsync(user) ? 310 : 311)!);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await userManager.UpdateAsync(user);
        }

        return await this.SignInResultAsync(user, returnUrl);
    }

    /// <summary>
    /// 사용자 로그아웃을 처리합니다 (갱신 토큰 무효화 및 토큰 쿠키 삭제).
    /// </summary>
    /// <returns>홈 이동 주소</returns>
    [HttpPost("signout")]
    public new async Task<IActionResult> SignOut()
    {
        await this.HttpContext.SignOutAccountAsync();
        return this.Complete(new { redirect = "/" });
    }

    #endregion

    #region Join & Mail Verification

    /// <summary>
    /// 가입 약관 목록을 출력 순서대로 조회합니다. 제목/본문은 다국어 출력 기본 규칙으로 고릅니다.
    /// </summary>
    /// <param name="withBody">본문 포함 여부</param>
    /// <returns>약관 목록 (key/text/essential/body)</returns>
    [HttpGet("terms")]
    public async Task<IActionResult> GetTerms([FromQuery] bool withBody = false)
    {
        var list = await this.Config!.GetJoinTermsAsync();
        var keys = list.Select(TermsKeyOf).ToArray();

        const string cacheKey = "account:terms:languages";
        var langs = await App.Cache.GetAsync<List<LanguageData>>(cacheKey);
        if (langs is null)
        {
            langs = await this.DbContext!.Languages.AsNoTracking()
                .Where(p => (p.Key == TermsTitleKey || p.Key == TermsBodyKey) && keys.Contains(p.Category))
                .ToListAsync();
            await App.Cache.SetAsync(cacheKey, langs, TimeSpan.FromMinutes(10));
        }

        string? Pick(string key, string langKey)
            => this.Request.PickCulture(langs.Where(t => t.Category == key && t.Key == langKey), t => t.Culture, t => t.Value);

        return this.Complete(list.Select(p =>
        {
            var key = TermsKeyOf(p);
            return new
            {
                key,
                text = Pick(key, TermsTitleKey) ?? key,
                essential = TermsEssentialOf(p),
                body = withBody ? Pick(key, TermsBodyKey) ?? string.Empty : null
            };
        }));
    }

    /// <summary>
    /// 회원 가입을 처리합니다. 화면 검증과 같은 규칙(ID 최소 길이·허용 문자, 관리 화면 비밀번호 규칙, 비밀번호 확인, ID/메일 중복, 필수 약관)을 서버에서 다시 검증한 뒤
    /// 사용자(현재 언어 표시명, 동의 약관 목록, 계정 구성의 가입 기본 그룹)를 만들고 메일 검증 구성(AccountRule.MailVerify)에 따라 로그인·이동을 처리합니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="dpName">표시명 (현재 언어로 저장)</param>
    /// <param name="email">메일</param>
    /// <param name="password">비밀번호</param>
    /// <param name="passwordConfirm">비밀번호 확인</param>
    /// <param name="terms">동의한 약관 키 목록 (반복 필드)</param>
    /// <param name="returnUrl">가입 후 돌아갈 요청 페이지 (사이트 내부 주소만 사용)</param>
    /// <returns>redirect: 이동할 주소, notice: 이동 전에 보일 안내 (없으면 null)</returns>
    [HttpPost("join")]
    [Valid("SignUp")]
    public async Task<IActionResult> Join([FromForm] string? uid, [FromForm] string? dpName, [FromForm] string? email,
        [FromForm] string? password, [FromForm] string? passwordConfirm, [FromForm] List<string>? terms, [FromForm] string? returnUrl)
    {
        var userName = uid!.Trim();
        var mail = email!.Trim();
        var errors = await this.JoinErrorsAsync(userName, mail, password!, passwordConfirm);
        if (errors.Count > 0) return this.Error(errors.Values.First(), new { ErrorCode = 330, Messages = errors });

        var agreed = await this.AgreedTermsAsync(terms);
        if (agreed is null) return this.Error(this.Lang("EssTermsRequired"));

        var user = await this.NewJoinUserAsync(userName, dpName!.Trim(), mail, password!, agreed);
        await this.GrantAdminIfFirstAsync(user);
        try
        {
            await this.AccountManager!.CreateUserAsync(user);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        this.HttpContext.Publish(ServiceEvents.MemberCreated, user.Id.ToString(), new Dictionary<string, string?> { ["by"] = "self" });

        var mailError = this.Config!.AccountRule.MailVerify == MailVerifyMode.Off ? null : await this.SendConfirmMailAsync(user, returnUrl);
        return await this.SignInResultAsync(user, returnUrl, mailError);
    }

    /// <summary>
    /// 메일 인증 메일을 다시 보냅니다. 이미 인증한 사용자는 거부하고, 같은 사용자에게는 <see cref="MailInterval"/> 안에 다시 보내지 않습니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="returnUrl">인증 후 돌아갈 요청 페이지</param>
    /// <returns>발송 결과</returns>
    [HttpPost("mail/resend")]
    public async Task<IActionResult> ResendConfirmMail([FromForm] string? uid, [FromForm] string? returnUrl)
    {
        var user = await this.FindUserAsync(uid);
        if (user is null) return this.NotFoundResult();
        if (user.EmailConfirmed) return this.Error(this.Lang("MailAlreadyVerified"));
        if (await App.Cache.ExistsAsync(MailCacheKey("confirm", user.Id.ToString()))) return this.Error(this.Lang("MailResendWait"));

        var mailError = await this.SendConfirmMailAsync(user, returnUrl);
        return mailError is null ? this.Complete(null, this.Lang("MailResent")) : this.Error(mailError);
    }

    /// <summary>
    /// 메일 인증 완료 여부만 확인합니다. 이 요청으로는 로그인하지 않으며(사용자 ID 만으로 로그인되면 안 됨),
    /// 인증되었으면 로그인 페이지(요청 페이지를 ReturnUrl 로 유지) 주소를 돌려줍니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="returnUrl">로그인 후 돌아갈 요청 페이지</param>
    /// <returns>verified, redirect (미인증이면 오류)</returns>
    [HttpPost("mail/check")]
    public async Task<IActionResult> CheckConfirmMail([FromForm] string? uid, [FromForm] string? returnUrl)
    {
        var user = await this.FindUserAsync(uid);
        if (user is null) return this.NotFoundResult();
        if (!user.EmailConfirmed) return this.Error(this.Lang("MailNotYetVerified"));

        var signPage = this.Config?.SignPage ?? "/Account/SignIn";
        var redirect = this.Url.IsLocalUrl(returnUrl) ? $"{signPage}?ReturnUrl={Uri.EscapeDataString(returnUrl!)}" : signPage;
        return this.Complete(new { verified = true, redirect }, this.Lang("MailVerifySuccess"));
    }

    #endregion

    #region Find ID / Reset Password

    /// <summary>
    /// 입력한 메일로 가입된 사용자에게 아이디 안내 메일을 보냅니다. 가입 여부를 드러내지 않도록 결과와 관계없이 같은 안내로 응답하고,
    /// 같은 메일 주소에는 <see cref="MailInterval"/> 안에 다시 보내지 않습니다.
    /// </summary>
    /// <param name="email">가입 시 등록한 이메일 주소</param>
    /// <returns>요청 접수 안내</returns>
    [HttpPost("find/id")]
    public async Task<IActionResult> FindId([FromForm] string? email)
    {
        var mail = email?.Trim();
        if (string.IsNullOrWhiteSpace(mail)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Email"))!);

        var normalized = mail.ToUpperInvariant();
        if (!await TryHoldMailAsync("findId", normalized)) return this.Error(this.Lang("MailResendWait"));

        var user = await this.HttpContext.GetService<IIdentityDbContext>()!.Users.Include(p => p.DpNames)
            .FirstOrDefaultAsync(p => p.NormalizedEmail == normalized && p.Enabler);
        if (user is not null) await this.SendTemplateMailAsync(user, MailFindIdTemplate, null);
        return this.Complete(null, this.Lang("FindIdMailSent"));
    }

    /// <summary>
    /// 아이디와 메일이 일치하는 사용자에게 비밀번호 재설정 링크 메일을 보냅니다. 가입 여부를 드러내지 않도록 결과와 관계없이 같은 안내로 응답하고,
    /// 같은 메일 주소에는 <see cref="MailInterval"/> 안에 다시 보내지 않습니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="email">등록된 이메일 주소</param>
    /// <returns>요청 접수 안내</returns>
    [HttpPost("find/password")]
    public async Task<IActionResult> RequestPasswordReset([FromForm] string? uid, [FromForm] string? email)
    {
        var ip = this.ClientIP ?? "unknown";
        var userName = uid?.Trim();
        var safeUid = userName ?? string.Empty;
        if (await this.IsResetThrottledAsync(ip, safeUid)) return this.Error(WebCodes._429, null, StatusCodes.Status429TooManyRequests);
        var mail = email?.Trim();
        if (string.IsNullOrWhiteSpace(userName)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("UserID"))!);
        if (string.IsNullOrWhiteSpace(mail)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Email"))!);

        var normalizedMail = mail.ToUpperInvariant();
        if (!await TryHoldMailAsync("resetPw", normalizedMail)) return this.Error(this.Lang("MailResendWait"));

        var normalizedUid = userName.ToUpperInvariant();
        var user = await this.HttpContext.GetService<IIdentityDbContext>()!.Users.Include(p => p.DpNames)
            .FirstOrDefaultAsync(p => p.NormalizedUserName == normalizedUid && p.NormalizedEmail == normalizedMail && p.Enabler);
        if (user is not null)
        {
            var token = await this.HttpContext.GetService<UserManager<AppUser>>()!.GeneratePasswordResetTokenAsync(user);
            var link = $"{this.Request.Scheme}://{this.Request.Host}/Account/ResetPassword?uid={Uri.EscapeDataString(user.UserName ?? string.Empty)}&token={Uri.EscapeDataString(token)}";
            await this.SendTemplateMailAsync(user, MailPwChangeTemplate, link);
        }
        return this.Complete(null, this.Lang("ResetPwMailSent"));
    }

    /// <summary>
    /// 비밀번호 재설정 토큰을 검증하고 새 비밀번호로 바꿉니다. 메일로 받은 링크로 들어왔으므로 메일 인증도 완료로 저장하고,
    /// 보안 스탬프가 바뀌므로 사용자 캐시를 지우고 갱신 토큰을 모두 무효화해 기존 로그인을 끊습니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="token">비밀번호 재설정 토큰</param>
    /// <param name="password">새 비밀번호</param>
    /// <param name="passwordConfirm">비밀번호 확인</param>
    /// <returns>redirect: 로그인 페이지, notice: 완료 안내</returns>
    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromForm] string? uid, [FromForm] string? token, [FromForm] string? password, [FromForm] string? passwordConfirm)
    {
        var ip = this.ClientIP ?? "unknown";
        var safeUid = uid?.Trim() ?? string.Empty;
        if (await this.IsResetThrottledAsync(ip, safeUid)) return this.Error(WebCodes._429, null, StatusCodes.Status429TooManyRequests);
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(token))
        {
            await this.RecordResetFailureAsync(ip, safeUid);
            return this.Error(this.Lang("ResetPwInvalidToken"));
        }
        if (string.IsNullOrWhiteSpace(password)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Password"))!);
        if (password != passwordConfirm) return this.Error(this.Lang("PasswordMismatch"));
        if (this.PasswordError(password) is string pwError) return this.Error(pwError);

        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByNameAsync(uid.Trim());
        if (user is null || !user.Enabler)
        {
            await this.RecordResetFailureAsync(ip, safeUid);
            return this.Error(this.Lang("ResetPwInvalidToken"));
        }

        // ResetPasswordAsync 가 비밀번호·보안 스탬프와 함께 메일 인증 상태도 저장한다
        user.EmailConfirmed = true;
        var result = await userManager.ResetPasswordAsync(user, token.Trim(), password);
        if (!result.Succeeded)
        {
            await this.RecordResetFailureAsync(ip, safeUid);
            return this.Error(this.Lang("ResetPwInvalidToken"));
        }
        await ClearResetFailureAsync(ip, safeUid);

        // 보안 스탬프가 바뀌었으므로 사용자 캐시를 지우고, 갱신 토큰(스탬프를 담지 않음)도 모두 무효화해 기존 로그인을 끊는다
        await this.AccountManager!.InvalidateUserCacheAsync(user.Id, user.UserName, user.Email);
        await this.AccountManager!.RevokeAllUserTokensAsync(user.Id);
        return this.Complete(new { redirect = this.Config?.SignPage ?? "/Account/SignIn", notice = this.Lang("ResetPwSuccess") });
    }

    /// <summary>
    /// 로그인 사용자의 비밀번호를 바꿉니다 (현재 비밀번호 확인). 보안 스탬프가 바뀌므로 다른 기기의 로그인(갱신 토큰)은 모두 끊고, 지금 기기는 새 토큰으로 다시 로그인합니다.
    /// </summary>
    /// <param name="currentPassword">현재 비밀번호</param>
    /// <param name="password">새 비밀번호</param>
    /// <param name="passwordConfirm">새 비밀번호 확인</param>
    /// <returns>처리 결과</returns>
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromForm] string? currentPassword, [FromForm] string? password, [FromForm] string? passwordConfirm)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        if (string.IsNullOrEmpty(currentPassword)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("CurrentPassword"))!);
        if (string.IsNullOrWhiteSpace(password)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Password"))!);
        if (password != passwordConfirm) return this.Error(this.Lang("PasswordMismatch"));
        if (this.PasswordError(password) is string pwError) return this.Error(pwError);

        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByIdAsync(me.ToString());
        if (user is null || !user.Enabler) return this.UnauthorizedResult();
        var result = await userManager.ChangePasswordAsync(user, currentPassword, password);
        if (!result.Succeeded) return this.Error(this.Lang("CurrentPasswordWrong"));

        await this.AccountManager!.InvalidateUserCacheAsync(user.Id, user.UserName, user.Email);
        await this.AccountManager!.RevokeAllUserTokensAsync(user.Id);
        await this.HttpContext.SignInAccountAsync(user);
        this.HttpContext.Publish(ServiceEvents.MemberUpdated, me.ToString(), new Dictionary<string, string?> { ["change"] = "password" });
        return this.Complete(message: this.Lang("PasswordChanged"));
    }

    /// <summary>
    /// 로그인 사용자의 내 정보(다국어 이름·메일·전화번호)를 저장합니다. 사용자 ID 와 설명(관리자 메모)은 바꾸지 않습니다.
    /// 메일을 바꾸면 메일 검증을 쓰는 경우(On/Warning) 미인증으로 돌리고 새 주소로 인증 메일을 보냅니다.
    /// </summary>
    /// <param name="dpNames">문화권별 표시 이름 JSON ({문화권: 이름}, 하나 이상, 각 64자 이내)</param>
    /// <param name="email">메일 (필수, 다른 사용자와 중복 불가)</param>
    /// <param name="phoneNumber">전화번호 (32자 이내)</param>
    /// <returns>처리 결과 (메일을 바꿨으면 재인증 안내)</returns>
    [HttpPut("profile")]
    public async Task<IActionResult> SaveProfile([FromForm] string? dpNames, [FromForm] string? email, [FromForm] string? phoneNumber)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var names = ParseMap(dpNames).Where(p => !string.IsNullOrWhiteSpace(p.Key) && !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => p.Key.Trim(), p => p.Value!.Clip(64), StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("DpName"))!);
        var mail = email?.Trim() ?? string.Empty;
        if (mail.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Email"))!);
        if (mail.Length > 256 || !MailAddress.TryCreate(mail, out _)) return this.Error(WebResources.ValidEmail.Formats(this.Lang("Email"))!);

        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var user = await db.Users.Include(p => p.DpNames).FirstOrDefaultAsync(p => p.Id == me && p.Enabler);
        if (user is null) return this.UnauthorizedResult();
        var oldMail = user.Email;
        var mailChanged = !string.Equals(oldMail, mail, StringComparison.OrdinalIgnoreCase);
        var normalized = mail.ToUpperInvariant();
        if (mailChanged && await db.Users.AnyAsync(p => p.Id != me && (p.NormalizedEmail == normalized || p.Email == mail))) // 정규화 값이 어긋난 기존 행도 막도록 원래 메일도 비교
            return this.Error(WebResources.ValidAlreadyItem.Formats(this.Lang("Email"))!);

        user.DpNames.RemoveAll(p => !names.ContainsKey(p.Culture));
        foreach (var (culture, name) in names)
        {
            if (user.DpNames.FirstOrDefault(p => p.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)) is { } dpName) dpName.Name = name;
            else user.DpNames.Add(new DpName<AppUser, Guid>(me, culture, name));
        }
        user.PhoneNumber = phoneNumber.Clip(32) is { Length: > 0 } phone ? phone : null;
        var verify = mailChanged && this.Config!.AccountRule.MailVerify != MailVerifyMode.Off;
        if (mailChanged)
        {
            user.Email = mail;
            user.NormalizedEmail = normalized;
            if (verify) user.EmailConfirmed = false; // 새 주소는 다시 인증해야 함
        }
        user.UDate = DateTime.UtcNow;
        await ((DbContext)db).SaveChangesAsync();
        await this.AccountManager!.InvalidateUserCacheAsync(me, user.UserName, oldMail);
        if (mailChanged) await this.AccountManager!.InvalidateUserCacheAsync(me, user.UserName, mail);
        this.HttpContext.Publish(ServiceEvents.MemberUpdated, me.ToString(), new Dictionary<string, string?> { ["change"] = "profile", ["mailChanged"] = mailChanged.ToString() });

        if (!verify) return this.Complete(message: WebResources.SavedComplate);
        var mailError = await this.SendConfirmMailAsync(user, "/user");
        return this.Complete(message: mailError ?? this.Lang("MailChangedVerify"));
    }

    /// <summary>
    /// 로그인 사용자가 회원 탈퇴합니다 (현재 비밀번호 확인, 외부 로그인으로만 가입해 비밀번호가 없으면 생략).관리자 삭제와 같은 처리(<see cref="IAccountCacheManager.DeleteUserAsync"/> — ID·메일 이관, 로그인 토큰 무효화)를 한 뒤 이 기기도 로그아웃합니다.
    /// 시스템 사용자는 거부됩니다.
    /// </summary>
    /// <param name="currentPassword">현재 비밀번호</param>
    /// <returns>redirect: 이동할 주소 (홈)</returns>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromForm] string? currentPassword)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();

        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByIdAsync(me.ToString());
        if (user is null || !user.Enabler) return this.UnauthorizedResult();
        // 외부 로그인으로만 가입해 비밀번호가 없는 회원은 확인할 비밀번호가 없다
        if (user.PasswordHash is not null)
        {
            if (string.IsNullOrEmpty(currentPassword)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("CurrentPassword"))!);
            if (!await userManager.CheckPasswordAsync(user, currentPassword)) return this.Error(this.Lang("CurrentPasswordWrong"));
        }
        try
        {
            await this.AccountManager!.DeleteUserAsync(me);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        await this.HttpContext.SignOutAccountAsync();
        this.HttpContext.Publish(ServiceEvents.MemberDeleted, me.ToString(), new Dictionary<string, string?> { ["by"] = "self" });
        return this.Complete(new { redirect = "/" }, this.Lang("WithdrawDone"));
    }

    #endregion

    #region External Login

    /// <summary>
    /// 외부 로그인 서비스가 확인해 준 사용자(<see cref="ExternalIdentity"/>, 콜백에서 보관)로 다음 단계를 처리합니다.
    /// 로그인 중이면 지금 계정에 연결하고, 연결된 계정이 있으면 그 계정으로 로그인(일반 로그인과 같은 비활성·잠금·메일 검증 처리), 없으면 가입에 쓸 값(join)을 돌려줍니다.
    /// 같은 메일의 기존 계정에 자동으로 연결하지 않습니다 (기존 회원은 로그인 후 내 정보에서 연결).
    /// </summary>
    /// <returns>redirect·notice (로그인·연결) 또는 join(provider, name, email)</returns>
    [HttpPost("external")]
    public async Task<IActionResult> ExternalSignIn()
    {
        if (await ExternalIdentity.LoadAsync(this.HttpContext) is not { } identity) return this.Error(this.Lang("ExternalExpired"));
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var linkedId = await db.UserLogins.Where(p => p.LoginProvider == identity.ProviderId && p.ProviderKey == identity.Key).Select(p => (Guid?)p.UserId).FirstOrDefaultAsync();

        if (this.CurrentUserId is { } me)
        {
            if (linkedId is { } other && other != me) return this.Error(this.Lang("ExternalLinkedOther"));
            if (linkedId is null) await this.AddLoginAsync(me, identity);
            await ExternalIdentity.ClearAsync(this.HttpContext);
            return this.Complete(new { redirect = "/user", notice = this.Lang("ExternalLinked") });
        }

        if (linkedId is null) return this.Complete(new { join = new { provider = identity.ProviderName, name = identity.Name, email = identity.Email } });
        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByIdAsync(linkedId.Value.ToString());
        if (user is null || !user.Enabler) return this.Error(WebCodes.Get(311)!);
        if (await userManager.IsLockedOutAsync(user)) return this.Error(WebCodes.Get(310)!);
        await ExternalIdentity.ClearAsync(this.HttpContext);
        return await this.SignInResultAsync(user, identity.ReturnUrl);
    }

    /// <summary>
    /// 외부 로그인으로 회원 가입합니다 (비밀번호 없음 — 나중에 비밀번호 찾기로 만들 수 있음). ID·메일 규칙과 필수 약관은 일반 가입과 같이 검증하고,
    /// 서비스가 검증한 메일을 그대로 쓰면 메일 인증을 마친 것으로 저장합니다. 만든 계정에 외부 로그인을 연결하고 로그인합니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="dpName">표시명 (현재 언어로 저장)</param>
    /// <param name="email">메일</param>
    /// <param name="terms">동의한 약관 키 목록 (반복 필드)</param>
    /// <returns>redirect: 이동할 주소, notice: 이동 전에 보일 안내 (없으면 null)</returns>
    [HttpPost("external/join")]
    [Valid("ExternalJoin")]
    public async Task<IActionResult> ExternalJoin([FromForm] string? uid, [FromForm] string? dpName, [FromForm] string? email, [FromForm] List<string>? terms)
    {
        if (await ExternalIdentity.LoadAsync(this.HttpContext) is not { } identity) return this.Error(this.Lang("ExternalExpired"));
        var userName = uid!.Trim();
        var mail = email!.Trim();
        var name = dpName!.Trim();
        var errors = await this.JoinErrorsAsync(userName, mail, null, null);
        if (errors.Count > 0) return this.Error(errors.Values.First(), new { ErrorCode = 330, Messages = errors });

        var agreed = await this.AgreedTermsAsync(terms);
        if (agreed is null) return this.Error(this.Lang("EssTermsRequired"));

        var user = await this.NewJoinUserAsync(userName, name, mail, null, agreed);
        user.EmailConfirmed = identity.EmailVerified && string.Equals(identity.Email, mail, StringComparison.OrdinalIgnoreCase);
        await this.GrantAdminIfFirstAsync(user);
        try
        {
            await this.AccountManager!.CreateUserAsync(user);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        await this.AddLoginAsync(user.Id, identity);
        await ExternalIdentity.ClearAsync(this.HttpContext);
        this.HttpContext.Publish(ServiceEvents.MemberCreated, user.Id.ToString(), new Dictionary<string, string?> { ["by"] = "self", ["external"] = identity.ProviderName });

        var mailError = user.EmailConfirmed || this.Config!.AccountRule.MailVerify == MailVerifyMode.Off ? null : await this.SendConfirmMailAsync(user, identity.ReturnUrl);
        return await this.SignInResultAsync(user, identity.ReturnUrl, mailError);
    }

    /// <summary>
    /// 로그인 사용자의 외부 로그인 연결 상태를 조회합니다: 사용 중인 서비스마다 연결 여부, 그리고 비밀번호가 있는지(없으면 마지막 연결은 해제 불가).
    /// </summary>
    /// <returns>providers(id, name, icon, linked), hasPassword</returns>
    [HttpGet("logins")]
    public async Task<IActionResult> GetLogins()
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var linked = await db.UserLogins.Where(p => p.UserId == me).Select(p => p.LoginProvider).ToListAsync();
        var hasPassword = await db.Users.Where(p => p.Id == me).Select(p => p.PasswordHash != null).FirstOrDefaultAsync();
        var providers = OAuthProvider.EnabledOf(this.Config).Select(p =>
        {
            var preset = p.Preset();
            return new { p.Id, p.Name, icon = preset.Icon, brandIcon = preset.BrandIcon, linked = linked.Contains(p.Id) };
        });
        return this.Complete(new { providers, hasPassword });
    }

    /// <summary>
    /// 로그인 사용자의 외부 로그인 연결 하나를 해제합니다. 비밀번호가 없는 계정의 마지막 연결은 해제할 수 없습니다(로그인 수단이 없어짐).
    /// </summary>
    /// <param name="provider">서비스 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("logins/{provider}")]
    public async Task<IActionResult> RemoveLogin(string provider)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var logins = await db.UserLogins.Where(p => p.UserId == me).ToListAsync();
        if (logins.FirstOrDefault(p => p.LoginProvider == provider) is not { } login) return this.NotFoundResult(WebResources.NotFoundData);
        if (logins.Count == 1 && !await db.Users.AnyAsync(p => p.Id == me && p.PasswordHash != null)) return this.Error(this.Lang("ExternalLastLogin"));
        db.UserLogins.Remove(login);
        await ((DbContext)db).SaveChangesAsync();
        return this.Complete(null, this.Lang("ExternalUnlinked"));
    }

    /// <summary>
    /// 사용자에게 외부 로그인을 연결합니다 (LoginProvider = 서비스 식별키, ProviderKey = 서비스 안 사용자 식별자).
    /// </summary>
    /// <param name="userId">연결할 사용자</param>
    /// <param name="identity">외부 로그인 사용자</param>
    private async Task AddLoginAsync(Guid userId, ExternalIdentity identity)
    {
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        db.UserLogins.Add(new IdentityUserLogin<Guid> { UserId = userId, LoginProvider = identity.ProviderId, ProviderKey = identity.Key, ProviderDisplayName = identity.ProviderName });
        await ((DbContext)db).SaveChangesAsync();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 로그인·가입 공통 마무리: 메일 검증 구성에 따라 토큰을 발급하고 이동 주소를 정합니다.
    /// On + 미인증이면 로그인하지 않고 메일 인증 페이지로, Warning + 미인증이면 로그인 후 안내, 그 외는 로그인 후 요청 페이지(없으면 홈)입니다.
    /// </summary>
    /// <param name="user">대상 사용자</param>
    /// <param name="returnUrl">복귀 대상 URL</param>
    /// <param name="mailError">인증 메일 발송 실패 안내 (없으면 null)</param>
    /// <returns>redirect, notice</returns>
    private async Task<IActionResult> SignInResultAsync(AppUser user, string? returnUrl, string? mailError = null)
    {
        var mode = this.Config?.AccountRule.MailVerify ?? MailVerifyMode.Off;
        if (!user.EmailConfirmed && mode == MailVerifyMode.On)
            return this.Complete(new { redirect = VerifyMailUrl(user.UserName ?? string.Empty, returnUrl, mailError is null), notice = (string?)null });

        await this.HttpContext.SignInAccountAsync(user);
        await this.ApplyUserSettingsAsync(user.Id);
        var notice = !user.EmailConfirmed && mode == MailVerifyMode.Warning ? mailError ?? this.Lang("MailNotYetVerified") : mailError;
        return this.Complete(new { redirect = this.Url.IsLocalUrl(returnUrl) ? returnUrl : "/", notice });
    }

    /// <summary>
    /// 사용자 환경설정 속성(화면 언어·테마)을 이 브라우저의 Culture·Theme 쿠키(1년, 환경설정 화면과 같은 쿠키)로 적용합니다 — 다른 기기에서 로그인해도 같은 언어·테마.
    /// </summary>
    /// <param name="userId">로그인한 사용자</param>
    private async Task ApplyUserSettingsAsync(Guid userId)
    {
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var settings = await db.UserProperties.AsNoTracking().Where(p => p.OwnerID == userId && (p.Key == UserController.CultureKey || p.Key == UserController.ThemeKey)).ToDictionaryAsync(p => p.Key, p => p.Value);
        var options = new CookieOptions { Path = "/", MaxAge = TimeSpan.FromDays(365), SameSite = SameSiteMode.Lax, IsEssential = true };
        if (settings.GetValueOrDefault(UserController.CultureKey) is { } culture && (this.Config?.Cultures ?? []).Contains(culture)) this.Response.Cookies.Append("Culture", culture, options);
        if (settings.GetValueOrDefault(UserController.ThemeKey) is "light" or "dark") this.Response.Cookies.Append("Theme", settings[UserController.ThemeKey]!, options);
    }

    /// <summary>
    /// 메일 인증/재발송 페이지 주소를 만듭니다. 인증 메일의 링크는 여기에 token 을 더한 주소입니다.
    /// </summary>
    /// <param name="userName">사용자 ID</param>
    /// <param name="returnUrl">인증 후 돌아갈 요청 페이지</param>
    /// <param name="sent">인증 메일 발송 성공 여부 (실패면 sent=false)</param>
    /// <returns>사이트 내부 주소</returns>
    private static string VerifyMailUrl(string userName, string? returnUrl, bool sent = true)
        => $"/Account/VerifyMail?uid={Uri.EscapeDataString(userName)}"
            + (string.IsNullOrEmpty(returnUrl) ? null : $"&returnUrl={Uri.EscapeDataString(returnUrl)}")
            + (sent ? null : "&sent=false");

    /// <summary>
    /// 사용자 ID 로 사용자(표시명 포함)를 찾습니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <returns>사용자 (없으면 null)</returns>
    private async Task<AppUser?> FindUserAsync(string? uid)
    {
        var normalized = uid?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(normalized) ? null
            : await this.HttpContext.GetService<IIdentityDbContext>()!.Users.Include(p => p.DpNames).FirstOrDefaultAsync(p => p.NormalizedUserName == normalized);
    }

    /// <summary>
    /// 가입 입력의 서버 검증 오류를 필드별로 모읍니다 (화면 검증과 같은 규칙, 같은 문구).
    /// </summary>
    /// <returns>필드 이름별 오류 문구 (없으면 빈 사전)</returns>
    private async Task<Dictionary<string, string>> JoinErrorsAsync(string userName, string email, string? password, string? passwordConfirm)
    {
        var errors = new Dictionary<string, string>();
        var rule = this.Config!.AccountRule;
        var allowed = this.CurrentIdentity.User.AllowedUserNameCharacters;
        if (userName.Length < rule.UidMinSize)
            errors["uid"] = WebResources.ValidMinLength.Formats(this.Lang("UserID"), rule.UidMinSize.ToString())!;
        else if (!string.IsNullOrEmpty(allowed) && userName.Any(c => !allowed.Contains(c)))
            errors["uid"] = WebResources.ValidRegex.Formats(this.Lang("UserID"))!;
        else if (await this.AccountManager!.GetUserByIDAsync(userName) is not null)
            errors["uid"] = WebResources.ValidAlreadyItem.Formats(this.Lang("UserID"))!;

        if (await this.AccountManager!.GetUserByMailAsync(email) is not null)
            errors["email"] = WebResources.ValidAlreadyItem.Formats(this.Lang("Email"))!;

        if (password is null) return errors; // 외부 로그인 가입: 비밀번호 없음
        if (this.PasswordError(password) is string passwordError) errors["password"] = passwordError;
        if (password != passwordConfirm) errors["passwordConfirm"] = this.Lang("PasswordMismatch");
        return errors;
    }

    /// <summary>현재 적용 중인 Identity 옵션 (DB/appsettings 반영 후 값)</summary>
    private IdentityOptions CurrentIdentity => this.HttpContext.GetService<IOptions<IdentityOptions>>()?.Value ?? new IdentityOptions();

    /// <summary>
    /// 관리 화면 구성(Identity 비밀번호 규칙)으로 비밀번호를 검사합니다. 판단 기준은 ASP.NET Identity PasswordValidator 와 같습니다(ASCII 영문/숫자 기준).
    /// </summary>
    /// <param name="password">비밀번호</param>
    /// <returns>오류 문구 (통과면 null)</returns>
    private string? PasswordError(string password)
    {
        var pw = this.CurrentIdentity.Password;
        if (password.Length < pw.RequiredLength) return WebResources.ValidMinLength.Formats(this.Lang("Password"), pw.RequiredLength.ToString());

        var needs = new[]
        {
            pw.RequireDigit && !password.Any(char.IsAsciiDigit) ? this.Lang("RequireDigit") : null,
            pw.RequireLowercase && !password.Any(char.IsAsciiLetterLower) ? this.Lang("RequireLowercase") : null,
            pw.RequireUppercase && !password.Any(char.IsAsciiLetterUpper) ? this.Lang("RequireUppercase") : null,
            pw.RequireNonAlphanumeric && password.All(char.IsAsciiLetterOrDigit) ? this.Lang("RequireNonAlphanumeric") : null
        }.OfType<string>().ToArray();
        if (needs.Length > 0) return this.Lang("PasswordNeeds").Formats(string.Join(", ", needs));
        return password.Distinct().Count() < pw.RequiredUniqueChars ? this.Lang("PasswordUniqueChars").Formats(pw.RequiredUniqueChars.ToString()) : null;
    }

    /// <summary>
    /// 동의한 약관을 현재 약관 목록으로 확인합니다. 목록에 없는 키는 무시합니다.
    /// </summary>
    /// <param name="terms">동의한 약관 키 목록</param>
    /// <returns>동의한 약관 키 목록 (필수 약관 미동의면 null)</returns>
    private async Task<List<string>?> AgreedTermsAsync(List<string>? terms)
    {
        var list = await this.Config!.GetJoinTermsAsync();
        var keys = (terms ?? []).ToHashSet(StringComparer.Ordinal);
        if (list.Any(p => TermsEssentialOf(p) && !keys.Contains(TermsKeyOf(p)))) return null;
        return list.Select(TermsKeyOf).Where(keys.Contains).Distinct().ToList();
    }

    /// <summary>약관 항목의 키</summary>
    private static string TermsKeyOf(JsonNode node) => node["key"]?.GetValue<string>() ?? string.Empty;

    /// <summary>약관 항목의 필수 여부</summary>
    private static bool TermsEssentialOf(JsonNode node) => node["essential"]?.GetValue<bool>() ?? false;

    /// <summary>
    /// 가입 사용자 엔티티를 만듭니다: 현재 언어 표시명, 비밀번호 해시(외부 로그인 가입은 없음), 동의 약관 목록(시스템 속성),
    /// 계정 구성의 가입 기본 그룹/역할 중 실제 있는 항목(기본 = 구성의 기본 항목, 없으면 첫 항목).
    /// </summary>
    /// <returns>저장 전 사용자 엔티티</returns>
    private async Task<AppUser> NewJoinUserAsync(string userName, string dpName, string email, string? password, List<string> agreed)
    {
        var user = new AppUser(userName, email) { LockoutEnabled = true };
        user.DpNames.Add(new DpName<AppUser, Guid>(user.Id, CultureInfo.CurrentUICulture.GetLastName(), dpName));
        if (agreed.Count > 0) user.Properties.Add(new Property<AppUser, Guid>(user.Id, JoinTermsAgreedKey, string.Join(",", agreed), true));
        user.Properties.Add(new Property<AppUser, Guid>(user.Id, UserController.CultureKey, CultureInfo.CurrentUICulture.GetLastName(), true)); // 가입할 때의 화면 언어·테마를 사용자 환경설정으로
        if (this.Request.Cookies["Theme"] is "light" or "dark") user.Properties.Add(new Property<AppUser, Guid>(user.Id, UserController.ThemeKey, this.Request.Cookies["Theme"]!, true));
        if (password is not null) user.PasswordHash = (this.HttpContext.GetService<IPasswordHasher<AppUser>>() ?? new PasswordHasher<AppUser>()).HashPassword(user, password);

        var rule = this.Config!.AccountRule;
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var (groupIds, defaultGroup) = await ExistingDefaultsAsync(db.Groups.Select(p => p.Id), rule.DefaultGroups, rule.DefaultGroupId);
        groupIds.ForEach(p => user.UserGroups.Add(new AppUserGroup(user.Id, p, p == defaultGroup, active: p == defaultGroup)));
        var (roleIds, defaultRole) = await ExistingDefaultsAsync(db.Roles.Select(p => p.Id), rule.DefaultRoles, rule.DefaultRoleId);
        roleIds.ForEach(p => user.UserRoles.Add(new AppUserRole(user.Id, p, p == defaultRole, active: p == defaultRole)));
        return user;
    }

    /// <summary>
    /// 가입 사용자가 첫 사용자(등록된 사용자 없음)이면 최종 관리자 역할(<see cref="SystemNames.SystemManagerRole"/>, 설치 시 등록)을 기본·활성 역할로 줍니다.
    /// 관리자 그룹(<see cref="SystemNames.ManagerGroup"/>)에도 넣습니다 (그 그룹에 연결된 일반 관리자 역할은 그룹을 통해 얻음). 최종 관리자 역할이 없으면 아무것도 하지 않습니다.
    /// </summary>
    /// <param name="user">저장 전 가입 사용자</param>
    private async Task GrantAdminIfFirstAsync(AppUser user)
    {
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        if (await db.Users.AnyAsync()) return;

        var name = SystemNames.SystemManagerRole.ToUpperInvariant();
        if (await db.Roles.Where(p => p.NormalizedName == name).Select(p => (Guid?)p.Id).FirstOrDefaultAsync() is not Guid adminId) return;

        user.UserRoles.ForEach(p => p.Default = p.Active = false);
        var link = user.UserRoles.FirstOrDefault(p => p.RoleId == adminId);
        if (link is null) user.UserRoles.Add(new AppUserRole(user.Id, adminId, true, active: true));
        else link.Default = link.Active = true;

        var managerGroup = SystemNames.ManagerGroup;
        if (await db.Groups.Where(p => p.Name == managerGroup && p.ParentId == null).Select(p => (Guid?)p.Id).FirstOrDefaultAsync() is Guid groupId && user.UserGroups.All(p => p.GroupId != groupId))
            user.UserGroups.Add(new AppUserGroup(user.Id, groupId, false, active: false));
    }

    /// <summary>
    /// 계정 구성의 가입 기본 연결 키 중 실제 있는 키와 기본 키(구성의 기본 키, 없으면 첫 키)를 구합니다.
    /// </summary>
    /// <param name="existing">대상 테이블 식별키 쿼리</param>
    /// <param name="ids">구성된 키 목록</param>
    /// <param name="defaultId">구성된 기본 키</param>
    /// <returns>실제 있는 키 목록, 기본 키 (목록이 비면 Guid.Empty)</returns>
    private static async Task<(List<Guid> Ids, Guid DefaultId)> ExistingDefaultsAsync(IQueryable<Guid> existing, List<Guid> ids, Guid? defaultId)
    {
        var requested = ids.Distinct().ToList();
        List<Guid> found = requested.Count == 0 ? [] : await existing.Where(p => requested.Contains(p)).ToListAsync();
        return (found, defaultId is Guid id && found.Contains(id) ? id : found.FirstOrDefault());
    }

    /// <summary>
    /// 인증 링크 메일을 보내고, 성공하면 재발송 간격 기록을 남깁니다.
    /// </summary>
    /// <param name="user">받는 사용자</param>
    /// <param name="returnUrl">인증 후 돌아갈 요청 페이지</param>
    /// <returns>실패 안내 문구 (성공이면 null)</returns>
    private async Task<string?> SendConfirmMailAsync(AppUser user, string? returnUrl)
    {
        var token = await this.HttpContext.GetService<UserManager<AppUser>>()!.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{this.Request.Scheme}://{this.Request.Host}{VerifyMailUrl(user.UserName ?? string.Empty, returnUrl)}&token={Uri.EscapeDataString(token)}";
        var error = await this.SendTemplateMailAsync(user, MailConfirmTemplate, link);
        if (error is null) await App.Cache.SetAsync(MailCacheKey("confirm", user.Id.ToString()), true, MailInterval);
        return error;
    }

    /// <summary>
    /// 계정 안내 메일을 관리 화면 템플릿으로 보냅니다 (메일 인증, 비밀번호 재설정, 아이디 찾기 공통).
    /// 치환 태그: {#domain} 사이트 주소, {#userid}, {#username}(표시명), {#usermail}, {#token}(링크 주소).
    /// 본문에 꼭 필요한 태그(링크가 있으면 {#token}, 없으면 {#userid})가 없으면 끝에 붙이고, HTML 본문이면 치환 값은 HTML 인코딩합니다.
    /// 본문의 에디터 이미지 상대 경로는 사이트 주소를 붙인 절대 경로로 바꿉니다.
    /// </summary>
    /// <param name="user">받는 사용자</param>
    /// <param name="templateKey">고정 템플릿 키</param>
    /// <param name="link">메일에 넣을 링크 (없으면 null)</param>
    /// <returns>실패 안내 문구 (성공이면 null)</returns>
    private async Task<string?> SendTemplateMailAsync(AppUser user, string templateKey, string? link)
    {
        var domain = $"{this.Request.Scheme}://{this.Request.Host}";
        var values = new Dictionary<string, string>
        {
            ["{#domain}"] = domain,
            ["{#userid}"] = user.UserName ?? string.Empty,
            ["{#username}"] = this.Request.DisplayName(user.DpNames, user.UserName),
            ["{#usermail}"] = user.Email ?? string.Empty,
            ["{#token}"] = link ?? string.Empty
        };
        var (title, body) = await this.MailTemplateAsync(templateKey);
        var isHtml = System.Text.RegularExpressions.Regex.IsMatch(body, "<[a-z][\\s\\S]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var required = link is null ? "{#userid}" : "{#token}";
        if (!body.Contains(required))
            body += !isHtml ? $"\n\n{required}" : link is null ? "<p><strong>{#userid}</strong></p>" : "<p><a href=\"{#token}\">{#token}</a></p>";
        string Fill(string text, bool encode) => values.Aggregate(text, (s, p) => s.Replace(p.Key, encode ? WebUtility.HtmlEncode(p.Value) : p.Value));
        // 메일 프로그램은 사이트 상대 경로를 열 수 없으므로 에디터 이미지 경로를 사이트 주소로 시작하게 바꾼다
        body = body.Replace($"\"{EditorFile.UrlPrefix}", $"\"{domain}{EditorFile.UrlPrefix}");

        try
        {
            await this.Config!.CreateMailSender().SendAsync(user.Email!, Fill(title, false), Fill(body, isHtml), isHtml);
            return null;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or NotSupportedException or FormatException or CryptographicException or IOException)
        {
            this.Logger?.LogWarning(ex, "Account mail {Template} to user {UserId} failed.", templateKey, user.Id);
            return this.Lang("MailNotSent");
        }
    }

    /// <summary>
    /// 메일 템플릿 제목/본문을 요청 문화권 기준으로 조회합니다.
    /// 저장된 템플릿을 [요청 문화권 → 대표 문화권 → 지원 문화권 → 저장된 첫 항목] 순으로 고르고, 없으면 리소스 기본 템플릿("{템플릿 키}Title|Body")을 씁니다.
    /// </summary>
    /// <param name="templateKey">고정 템플릿 키</param>
    /// <returns>제목, 본문 ({#…} 치환 전)</returns>
    private async Task<(string Title, string Body)> MailTemplateAsync(string templateKey)
    {
        var cacheKey = $"template:mail:{templateKey}";
        var saved = await App.Cache.GetAsync<Dictionary<string, Dictionary<string, string>>>(cacheKey);
        if (saved is null)
        {
            saved = await this.LoadLanguagesAsync(WebConfig.AccountCategory, $"{templateKey}@Title", $"{templateKey}@Body");
            await App.Cache.SetAsync(cacheKey, saved, TimeSpan.FromMinutes(10));
        }

        string Pick(string key, string defaultKey)
            => this.Request.PickCulture(saved.GetValueOrDefault(key)?.Where(p => !string.IsNullOrWhiteSpace(p.Value)), p => p.Key, p => p.Value) is { Length: > 0 } text
                ? text : this.Lang(defaultKey);

        return (Pick($"{templateKey}@Title", templateKey + "Title"), Pick($"{templateKey}@Body", templateKey + "Body"));
    }

    /// <summary>
    /// 같은 대상에게 보내는 안내 메일의 간격을 확인하고, 보낼 수 있으면 간격 기록을 남깁니다.
    /// 가입 여부를 드러내지 않도록 사용자가 없을 때도 기록합니다.
    /// </summary>
    /// <param name="kind">메일 종류</param>
    /// <param name="target">대상 (정규화한 메일 주소 등)</param>
    /// <returns>보낼 수 있으면 true, 간격 안이면 false</returns>
    private static async Task<bool> TryHoldMailAsync(string kind, string target)
    {
        var key = MailCacheKey(kind, target);
        if (await App.Cache.ExistsAsync(key)) return false;
        await App.Cache.SetAsync(key, true, MailInterval);
        return true;
    }

    /// <summary>안내 메일 재발송 간격 기록 캐시 키</summary>
    /// <param name="kind">메일 종류</param>
    /// <param name="target">대상</param>
    /// <returns>캐시 키</returns>
    private static string MailCacheKey(string kind, string target) => $"account:mail:{kind}:{target}";

    private static string ResetAttemptsKey(string ip, string uid) => $"account:ratelimit:resetpw:{ip}:{uid.ToUpperInvariant()}";

    private async Task<bool> IsResetThrottledAsync(string ip, string uid)
    {
        var count = await App.Cache.GetAsync<int>(ResetAttemptsKey(ip, uid));
        return count >= MaxResetAttempts;
    }

    private async Task RecordResetFailureAsync(string ip, string uid)
    {
        var key = ResetAttemptsKey(ip, uid);
        var count = await App.Cache.GetAsync<int>(key);
        await App.Cache.SetAsync(key, count + 1, ResetLockoutDuration);
    }

    private static async Task ClearResetFailureAsync(string ip, string uid) => await App.Cache.RemoveAsync(ResetAttemptsKey(ip, uid));

    #endregion
}
