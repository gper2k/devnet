using BitPlus.Data.Account.Model;
using BitPlus.Web.HTTP.View;
using BitPlus.Web.Site.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 회원 로그인, 회원가입 등 계정 인증 관련 뷰를 렌더링하는 MVC 컨트롤러입니다.
/// </summary>
public class AccountController : ViewControllerBase
{
    /// <summary>
    /// <see cref="AccountController"/> 기본 생성자.
    /// </summary>
    public AccountController() : base() { }

    /// <inheritdoc/>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        var action = context.ActionDescriptor.RouteValues["action"];
        // 로그아웃, 외부 로그인, 레이아웃 편집 모드(?editable=1)는 로그인 사용자도 연다
        if (action is "SignOut" or "ExternalLogin" or "External") return;
        if (string.Equals(context.HttpContext.Request.Query["editable"], "1", StringComparison.OrdinalIgnoreCase)) return;

        if (this.IsAuthenticated)
        {
            if (string.Equals(action, "VerifyMail", StringComparison.OrdinalIgnoreCase)
                && this.CurrentUser?.EmailConfirmed == false) return;

            var returnUrl = context.HttpContext.Request.Query["ReturnUrl"].ToString();
            var target = !string.IsNullOrEmpty(returnUrl) && this.Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            context.Result = this.Redirect(target);
        }
    }

    /// <summary>
    /// 로그인 페이지 뷰를 반환합니다.
    /// </summary>
    /// <returns>로그인 뷰 결과</returns>
    [HttpGet]
    public IActionResult SignIn()
    {
        this.SetPageKey("SignIn");
        return this.View();
    }

    /// <summary>
    /// 회원가입 페이지 뷰를 반환합니다.
    /// </summary>
    /// <returns>회원가입 뷰 결과</returns>
    [HttpGet]
    public IActionResult SignUp()
    {
        this.SetPageKey("SignUp");
        return this.View();
    }

    /// <summary>
    /// ID/PW 찾기 페이지 뷰를 반환합니다.
    /// </summary>
    /// <param name="tab">기본 활성 탭 (id 또는 pw)</param>
    /// <returns>ID/PW 찾기 뷰 결과</returns>
    [HttpGet]
    public IActionResult FindAccount([FromQuery] string? tab = "id")
    {
        this.SetPageKey("FindAccount");
        this.ViewData["ActiveTab"] = tab?.ToLowerInvariant() == "pw" ? "pw" : "id";
        return this.View();
    }

    /// <summary>
    /// 비밀번호 재설정 페이지 뷰를 반환합니다 (메일 링크를 통해 토큰과 함께 진입).
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="token">비밀번호 재설정 토큰</param>
    /// <returns>비밀번호 재설정 뷰 결과</returns>
    [HttpGet]
    public IActionResult ResetPassword([FromQuery] string? uid, [FromQuery] string? token)
    {
        this.SetPageKey("ResetPassword");
        this.ViewData["Uid"] = uid;
        this.ViewData["Token"] = token;
        return this.View();
    }

    /// <summary>
    /// 메일 인증/재발송 페이지. 인증 메일의 링크(token 포함)로 들어오면 인증을 완료하고 로그인한 뒤 요청 페이지(없으면 홈)로 이동합니다.
    /// token 이 없거나 인증에 실패하면 안내와 재발송 버튼을 보여 줍니다. 이미 인증한 사용자는 요청 페이지로 이동합니다.
    /// </summary>
    /// <param name="uid">사용자 ID</param>
    /// <param name="token">메일 인증 토큰</param>
    /// <param name="returnUrl">인증 후 돌아갈 요청 페이지 (사이트 내부 주소만 사용)</param>
    /// <param name="sent">가입 시 인증 메일 발송 성공 여부</param>
    /// <returns>뷰 또는 리다이렉트 결과</returns>
    [HttpGet]
    public async Task<IActionResult> VerifyMail(string? uid, string? token, string? returnUrl, bool sent = true)
    {
        var user = string.IsNullOrWhiteSpace(uid) ? null : await this.AccountManager!.GetUserByIDAsync(uid.Trim());
        if (user is null) return this.Redirect("/");
        var target = this.Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        if (user.EmailConfirmed) return this.Redirect(target);

        if (!string.IsNullOrEmpty(token))
        {
            var cleanToken = token.Trim().Replace(" ", "+");
            var confirmed = await this.ConfirmMailAsync(user.Id, cleanToken);
            if (confirmed is not null)
            {
                await this.HttpContext.SignInAccountAsync(confirmed);
                return this.Redirect(target);
            }
        }

        this.ViewData["Uid"] = user.UserName;
        this.ViewData["Mail"] = user.Email.MaskMail();
        this.ViewData["ReturnUrl"] = this.Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        this.ViewData["State"] = !string.IsNullOrEmpty(token) ? "failed" : sent ? "sent" : "notSent";
        return this.View();
    }


    /// <summary>
    /// 외부 로그인 서비스의 인증 화면으로 보냅니다 (관리 화면 "OAuth 구성" 에서 사용 중인 서비스만). 돌아오면 콜백이 사용자 정보를 보관하고 <see cref="External"/> 로 보냅니다.
    /// 로그인 중에 열면 돌아온 뒤 지금 계정에 연결합니다.
    /// </summary>
    /// <param name="provider">서비스 식별키</param>
    /// <param name="returnUrl">로그인 후 돌아갈 요청 페이지 (사이트 내부 주소만 사용)</param>
    /// <returns>인증 요청(Challenge) 또는 로그인 페이지 리다이렉트</returns>
    [HttpGet]
    public IActionResult ExternalLogin(string? provider, string? returnUrl)
    {
        if (OAuthProvider.EnabledOf(this.Config).FirstOrDefault(p => p.Id == provider) is not { } item) return this.Redirect("/Account/SignIn?external=failed");
        var properties = new AuthenticationProperties();
        if (this.Url.IsLocalUrl(returnUrl)) properties.Items[OAuthLoginEvents.ReturnUrlItem] = returnUrl;
        return this.Challenge(properties, item.SchemeName());
    }

    /// <summary>
    /// 외부 로그인 화면 (회원가입 뷰의 외부 로그인 모드): 화면이 보관된 외부 사용자로 로그인·연결을 요청하고(api/account/external), 연결된 계정이 없으면 비밀번호 없는 가입 양식(ID·이름·메일·약관)을 보여 줍니다.
    /// </summary>
    /// <returns>회원가입 뷰 (외부 로그인 모드)</returns>
    [HttpGet]
    public IActionResult External()
    {
        this.SetPageKey("External");
        this.ViewData["External"] = true;
        return this.View("SignUp");
    }

    /// <summary>
    /// 로그아웃 처리(갱신 토큰 무효화, 토큰 쿠키 삭제) 후 메인 홈으로 리다이렉트합니다.
    /// </summary>
    /// <returns>홈 리다이렉트 결과</returns>
    [HttpGet]
    public new async Task<IActionResult> SignOut()
    {
        await this.HttpContext.SignOutAccountAsync();
        return this.Redirect("/");
    }

    /// <summary>
    /// 메일 인증 토큰을 확인해 인증 완료로 저장하고 사용자 캐시를 지웁니다.
    /// </summary>
    /// <param name="userId">사용자 식별키</param>
    /// <param name="token">메일 인증 토큰</param>
    /// <returns>인증된 사용자 (토큰이 틀리거나 만료되면 null)</returns>
    private async Task<AppUser?> ConfirmMailAsync(Guid userId, string token)
    {
        var userManager = this.HttpContext.GetService<UserManager<AppUser>>()!;
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !(await userManager.ConfirmEmailAsync(user, token)).Succeeded) return null;
        await this.AccountManager!.InvalidateUserCacheAsync(user.Id, user.UserName, user.Email);
        return user;
    }
}
