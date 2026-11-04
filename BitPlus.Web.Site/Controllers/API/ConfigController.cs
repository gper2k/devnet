using BitPlus.Data.Cache;
using BitPlus.Net;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Security.Validation;
using BitPlus.Web.Site.Ai;
using BitPlus.Web.Site.Data;
using BitPlus.Web.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Globalization;
using System.Net.Mail;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "시스템 설정" 화면용 전역 구성 Web API 컨트롤러 (api/config).
/// 탭(설정 묶음)이 늘어나면 이 컨트롤러에 해당 구성의 조회/저장 액션을 추가합니다. 요청은 개별 폼 필드로 받습니다.
/// </summary>
public partial class ConfigController : APIControllerBase
{
    /// <summary>
    /// <see cref="ConfigController"/> 기본 생성자.
    /// </summary>
    public ConfigController() : base() { }

    /// <summary>사이트 정보 이미지(파비콘·공유 이미지·로고)를 에디터 파일로 올릴 때의 소유자 형식 (소유자 키 = <see cref="SiteInfo.Key"/>)</summary>
    public const string SiteInfoOwnerType = "site.info";

    /// <summary>
    /// 사이트 기본 정보를 조회합니다 (저장된 값이 없으면 기본값). 관리자 정책만.
    /// </summary>
    /// <returns><see cref="SiteInfo"/> 의 모든 항목</returns>
    [HttpGet("site")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetSite() => this.Complete(SiteInfo.Of(this.Config));

    /// <summary>
    /// 사이트 기본 정보를 저장합니다 (설정 "SiteInfo", AppConfig 범주). 사이트명·카피라이트는 간단한 HTML 을 허용하되 정제하고, 이미지 경로는 사이트 경로(<c>/</c>)나 http(s) 주소만 받습니다.
    /// 이 화면에서 올린 이미지(에디터 파일, 소유자 <see cref="SiteInfoOwnerType"/>)는 저장된 경로에 쓰인 것만 등록하고 나머지는 지웁니다.
    /// </summary>
    /// <param name="title">사이트 기본 제목 (필수)</param>
    /// <param name="keywords">검색 키워드</param>
    /// <param name="description">사이트 설명</param>
    /// <param name="favicon">파비콘 경로</param>
    /// <param name="shareImage">공유 미리보기 기본 이미지 경로</param>
    /// <param name="logo">로고 이미지 경로</param>
    /// <param name="siteName">사이트명 (간단한 HTML)</param>
    /// <param name="slogan">하단 소개 문구</param>
    /// <param name="companyName">회사명</param>
    /// <param name="phone">회사 전화</param>
    /// <param name="email">회사 메일</param>
    /// <param name="address">회사 주소</param>
    /// <param name="copyright">카피라이트 (간단한 HTML)</param>
    /// <param name="generalLayout">일반 레이아웃 이름</param>
    /// <param name="accountLayout">인증 레이아웃 이름</param>
    /// <param name="editor">주 에디터 공급자 이름 (영문 소문자·숫자, 비우면 클라이언트 기본)</param>
    /// <returns>처리 결과</returns>
    [HttpPut("site")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveSite([FromForm] string? title, [FromForm] string? keywords, [FromForm] string? description, [FromForm] string? favicon, [FromForm] string? shareImage,
        [FromForm] string? logo, [FromForm] string? siteName, [FromForm] string? slogan, [FromForm] string? companyName, [FromForm] string? phone, [FromForm] string? email,
        [FromForm] string? address, [FromForm] string? copyright, [FromForm] string? generalLayout, [FromForm] string? accountLayout, [FromForm] string? editor)
    {
        title = title?.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > 128) return this.Error(WebResources.ValidRequired.Formats(this.Lang("SiteTitle"))!);
        foreach (var path in new[] { favicon, shareImage, logo })
            if (!IsImagePath(path)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("Image"))!);
        editor = editor?.Trim().ToLowerInvariant() ?? string.Empty; // 공급자 목록은 클라이언트(site.ts)에 있으므로 형식만 확인, 모르는 이름은 클라이언트가 기본 에디터로 대신함
        if (editor.Length > 32 || !editor.All(char.IsAsciiLetterOrDigit)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("MainEditor"))!);

        var info = new SiteInfo
        {
            Title = title,
            Keywords = keywords.Clip(512),
            Description = description.Clip(512),
            Favicon = favicon.Clip(512),
            ShareImage = shareImage.Clip(512),
            Logo = logo.Clip(512),
            SiteName = EditorFile.Sanitize(siteName.Clip(256)),
            Slogan = slogan.Clip(512),
            CompanyName = companyName.Clip(128),
            Phone = phone.Clip(64),
            Email = email.Clip(128),
            Address = address.Clip(256),
            Copyright = EditorFile.Sanitize(copyright.Clip(512)),
            GeneralLayout = generalLayout.Clip(64) ?? "Default",
            AccountLayout = accountLayout.Clip(64) ?? "Default",
            Editor = editor
        };
        var db = this.DbContext!;
        await this.Config!.SetModelAsync(db, SiteInfo.Key, info);
        await EditorFile.SyncAsync(db, this.Config.UploadFolder, SiteInfoOwnerType, SiteInfo.Key, string.Join("", new[] { info.Favicon, info.ShareImage, info.Logo }.Where(p => p.Length > 0).Select(p => $"<img src=\"{p}\">")), false); // 쓰인 이미지만 등록, 나머지(바꾸기 전 이미지 등) 삭제
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 사이트 언어 구성을 조회합니다: 기본 문화권(설정 "Culture"), 지원 문화권 순서(설정 "Cultures"), 고를 수 있는 언어 목록(.NET 중립 문화권). 관리자 정책만.
    /// </summary>
    /// <returns>culture, cultures, available([{code, name}])</returns>
    [HttpGet("cultures")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetCultures() => this.Complete(new
    {
        culture = this.Config!.Culture,
        cultures = this.Config.Cultures,
        available = CultureInfo.GetCultures(CultureTypes.NeutralCultures).Where(p => p.Name.Length > 0).OrderBy(p => p.Name).Select(p => new { code = p.Name, name = p.NativeName })
    });

    /// <summary>
    /// 사이트 언어 구성을 저장합니다 (설정 "Cultures" = 쉼표로 이은 순서, "Culture" = 기본). 문화권 코드는 .NET 이 아는 것만, 중복은 하나로, 기본 문화권은 지원 목록에 있어야 합니다.
    /// 설정 저장이 캐시를 지우므로 문화권 판정(<c>UseCulture</c>)·언어 메뉴는 다음 요청부터 새 구성을 씁니다.
    /// </summary>
    /// <param name="culture">기본 문화권</param>
    /// <param name="cultures">지원 문화권 (순서대로, 반복 필드)</param>
    /// <returns>처리 결과</returns>
    [HttpPut("cultures")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveCultures([FromForm] string? culture, [FromForm] List<string>? cultures)
    {
        var known = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(p => p.Name.Length > 0).ToDictionary(p => p.Name, p => p.Name, StringComparer.OrdinalIgnoreCase); // ICU 는 아무 이름이나 문화권으로 만들어 주므로 목록으로 확인
        var asked = (cultures ?? []).Select(p => p.Trim()).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var list = asked.Select(p => known.GetValueOrDefault(p)).OfType<string>().ToList();
        if (list.Count == 0 || list.Count != asked.Count) return this.Error(WebResources.ValidRegex.Formats(this.Lang("SupportedLanguages"))!);
        var main = list.FirstOrDefault(p => p.NEqual(culture));
        if (main is null) return this.Error(WebResources.ValidRegex.Formats(this.Lang("DefaultLanguage"))!); // 기본 문화권은 지원 목록에서 지울 수 없음
        var db = this.DbContext!;
        await this.Config!.SetAsync(db, "Cultures", string.Join(',', list));
        await this.Config.SetAsync(db, "Culture", main);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 사용 가능한 레이아웃 목록(General, Account)을 조회합니다.
    /// </summary>
    /// <returns>범주별 사용 가능한 레이아웃 목록</returns>
    [HttpGet("layouts")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetLayouts() => this.Complete(new
    {
        general = SiteInfo.AvailableLayouts("General"),
        account = SiteInfo.AvailableLayouts("Account")
    });

    /// <summary>
    /// 지정된 범주 및 레이아웃의 테마 색상 설정을 조회합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <returns>레이아웃 테마 색상 설정</returns>
    [HttpGet("layout/{category}/{name}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetLayoutTheme(string category, string name)
    {
        var key = LayoutThemeConfig.ConfigKey(category, name);
        var config = this.Config?.GetModel<LayoutThemeConfig>(key) ?? new LayoutThemeConfig();
        return this.Complete(config);
    }

    /// <summary>
    /// 지정된 범주 및 레이아웃의 테마 색상 설정을 저장합니다 (최종 관리자 권한).
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <param name="model">저장할 테마 색상 설정</param>
    /// <returns>저장 결과</returns>
    [HttpPut("layout/{category}/{name}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveLayoutTheme(string category, string name, [FromBody] LayoutThemeConfig model)
    {
        if (this.CurrentUserAssets?.IsInRole(SystemNames.SystemManagerRole) != true && !this.IsManager)
            return this.ForbiddenResult();

        var key = LayoutThemeConfig.ConfigKey(category, name);
        await this.Config!.SetModelAsync(this.DbContext!, key, model);
        return this.Complete(model, message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 지정된 범주 및 레이아웃의 테마 색상 설정을 초기화(삭제)합니다 (최종 관리자 권한).
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <returns>초기화 결과</returns>
    [HttpDelete("layout/{category}/{name}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> DeleteLayoutTheme(string category, string name)
    {
        if (this.CurrentUserAssets?.IsInRole(SystemNames.SystemManagerRole) != true && !this.IsManager)
            return this.ForbiddenResult();

        var key = LayoutThemeConfig.ConfigKey(category, name);
        var existing = await this.DbContext!.Settings.FirstOrDefaultAsync(p => p.Key == key);
        if (existing != null)
        {
            this.DbContext.Settings.Remove(existing);
            await ((DbContext)this.DbContext).SaveChangesAsync();
        }
        await this.Config!.InvalidateAsync(key);
        return this.Complete(null, message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 레이아웃 전용 설정 파샬뷰(Config.cshtml)를 반환합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <param name="editable">편집 모드 쿼리 (1)</param>
    /// <returns>설정 UI 파샬뷰</returns>
    [HttpGet("layout/view/{category}/{name}")]
    public IActionResult GetLayoutConfigView(string category, string name, [FromQuery] string? editable)
    {
        var isManager = this.CurrentUserAssets?.IsInRole(SystemNames.SystemManagerRole) == true || this.IsManager;
        var isEditable = editable == "1" || this.Request.Query["editable"] == "1";
        if (!isManager && !isEditable) return this.ForbiddenResult();

        var path = $"~/Views/Layouts/{category}/{name}/Config.cshtml";
        if (!SiteInfo.FileExistsOrCompiled(path)) return this.NotFoundResult();
        var key = LayoutThemeConfig.ConfigKey(category, name);
        var config = this.Config?.GetModel<LayoutThemeConfig>(key);
        return this.PartialView(path, config);
    }

    /// <summary>
    /// 외부 서비스 연동 정보를 조회합니다 (설정 "SiteIntegration").
    /// </summary>
    /// <returns><see cref="SiteIntegration"/> 의 모든 항목</returns>
    [HttpGet("integration")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetIntegration() => this.Complete(SiteIntegration.Of(this.Config));

    /// <summary>
    /// 외부 서비스 연동 정보를 저장합니다 (설정 "SiteIntegration", AppConfig 범주). 키는 영문·숫자만 받습니다(비우면 연동 안 함).
    /// </summary>
    /// <param name="kakaoJsKey">카카오 JavaScript 키</param>
    /// <param name="gaMeasurementId">Google Analytics 4 측정 ID (<c>G-</c> + 영문·숫자)</param>
    /// <returns>저장 결과</returns>
    [HttpPut("integration")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveIntegration([FromForm] string? kakaoJsKey, [FromForm] string? gaMeasurementId)
    {
        var kakao = kakaoJsKey.Clip(64);
        if (!kakao.All(char.IsAsciiLetterOrDigit)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("KakaoJsKey"))!);
        var ga = gaMeasurementId.Clip(32).ToUpperInvariant();
        if (ga.Length > 0 && !(ga.StartsWith("G-") && ga.Length > 2 && ga[2..].All(char.IsAsciiLetterOrDigit))) return this.Error(WebResources.ValidRegex.Formats(this.Lang("GaMeasurementId"))!);
        await this.Config!.SetModelAsync(this.DbContext!, SiteIntegration.Key, new SiteIntegration { KakaoJsKey = kakao, GaMeasurementId = ga });
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>이미지 경로로 받을 수 있는 값인지 (비었거나, 사이트 경로 <c>/…</c>(<c>//</c> 제외), http(s) 주소)</summary>
    private static bool IsImagePath(string? path)
    {
        path = path?.Trim();
        return string.IsNullOrEmpty(path) || (path.StartsWith('/') && !path.StartsWith("//") && !path.Contains('"'))
            || (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && !path.Contains('"'));
    }


    /// <summary>
    /// 메일 발송용 SMTP 구성을 조회합니다. 비밀번호는 내려주지 않고 저장 여부(smtpHasPassword)만 알려줍니다.
    /// </summary>
    /// <returns>smtpHost/smtpPort/smtpSsl/smtpUser/smtpFrom/smtpFromName/smtpHasPassword</returns>
    [HttpGet("smtp")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetSmtp()
    {
        var smtp = this.Config!.AccountSmtp;
        return this.Complete(new
        {
            smtpHost = smtp.Host,
            smtpPort = smtp.Port,
            smtpSsl = smtp.UseSsl,
            smtpUser = smtp.UserName,
            smtpFrom = smtp.From,
            smtpFromName = smtp.FromName,
            smtpHasPassword = !string.IsNullOrEmpty(smtp.Password)
        });
    }

    /// <summary>
    /// 메일 발송용 SMTP 구성을 저장합니다(설정 "AccountSmtp", Account 범주). 전달되지 않은 항목은 현재 값을 유지하고,
    /// 비밀번호는 비워 두면 기존 값을 유지하며 입력 시 ASP.NET Data Protection 으로 보호(암호화)해 저장합니다.
    /// </summary>
    /// <param name="smtpHost">SMTP 서버</param>
    /// <param name="smtpPort">SMTP 포트</param>
    /// <param name="smtpSsl">SSL/TLS 사용</param>
    /// <param name="smtpUser">SMTP 계정</param>
    /// <param name="smtpPassword">SMTP 비밀번호 (비우면 기존 값 유지)</param>
    /// <param name="smtpFrom">보내는 메일 주소</param>
    /// <param name="smtpFromName">보내는 사람 이름</param>
    /// <returns>처리 결과</returns>
    [HttpPut("smtp")]
    [Auth(WebConfig.ManagerPolicyName)]
    [Valid("SmtpSave")]
    public async Task<IActionResult> SaveSmtp([FromForm] string? smtpHost, [FromForm] int? smtpPort, [FromForm] bool? smtpSsl,
        [FromForm] string? smtpUser, [FromForm] string? smtpPassword, [FromForm] string? smtpFrom, [FromForm] string? smtpFromName)
    {
        // 캐시된 인스턴스를 직접 바꾸지 않도록 새 객체에 현재 값 + 요청 값을 담는다.
        var current = this.Config!.AccountSmtp;
        var smtp = new SmtpConfig
        {
            Host = smtpHost?.Trim() ?? current.Host,
            Port = smtpPort ?? current.Port,
            UseSsl = smtpSsl ?? current.UseSsl,
            UserName = smtpUser?.Trim() ?? current.UserName,
            Password = string.IsNullOrEmpty(smtpPassword) ? current.Password : this.Config!.ProtectSmtpPassword(smtpPassword),
            From = smtpFrom?.Trim() ?? current.From,
            FromName = smtpFromName?.Trim() ?? current.FromName
        };

        await this.Config!.AccountConfig.SetModelAsync(this.DbContext!, WebConfig.AccountSmtpKey, smtp);
        await this.Config!.AccountConfig.InvalidateAsync(WebConfig.AccountSmtpKey);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 화면에 입력된 SMTP 값(저장 전 값 포함)으로 <see cref="SmtpSender"/> 를 통해 테스트 메일을 보냅니다. 비밀번호를 비워 두면 저장된 비밀번호를 씁니다.
    /// 성공/실패는 결과 메시지로 알려주며, 설정은 저장하지 않습니다.
    /// </summary>
    /// <param name="smtpHost">SMTP 서버</param>
    /// <param name="smtpPort">SMTP 포트</param>
    /// <param name="smtpSsl">SSL/TLS 사용</param>
    /// <param name="smtpUser">SMTP 계정</param>
    /// <param name="smtpPassword">SMTP 비밀번호 (비우면 저장된 값)</param>
    /// <param name="smtpFrom">보내는 메일 주소</param>
    /// <param name="smtpFromName">보내는 사람 이름</param>
    /// <param name="testTo">테스트 메일 받을 주소</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>처리 결과</returns>
    [HttpPost("smtp/test")]
    [Auth(WebConfig.ManagerPolicyName)]
    [Valid("SmtpSave")]
    public async Task<IActionResult> TestSmtp([FromForm] string? smtpHost, [FromForm] int? smtpPort, [FromForm] bool? smtpSsl,
        [FromForm] string? smtpUser, [FromForm] string? smtpPassword, [FromForm] string? smtpFrom, [FromForm] string? smtpFromName,
        [FromForm] string? testTo, CancellationToken cancellationToken = default)
    {
        var toName = this.Request.GetLanguage("TestMailTo") ?? "Test recipient";
        if (string.IsNullOrWhiteSpace(testTo) || !MailAddress.TryCreate(testTo.Trim(), out var to))
            return this.Error(WebResources.ValidEmail.Formats(toName)!);

        // 화면 입력값(저장 전 포함) + 저장된 값으로 발송 구성을 만들고, 알려진 서비스 특화 처리는 SmtpSender 가 맡는다
        var saved = this.Config!.AccountSmtp;
        var source = new SmtpConfig
        {
            Host = smtpHost?.Trim() ?? saved.Host,
            Port = smtpPort ?? saved.Port,
            UseSsl = smtpSsl ?? saved.UseSsl,
            UserName = smtpUser?.Trim() ?? saved.UserName,
            From = smtpFrom?.Trim() ?? saved.From,
            FromName = smtpFromName?.Trim() ?? saved.FromName
        };
        if (string.IsNullOrWhiteSpace(source.From) || (string.IsNullOrWhiteSpace(source.Host) && string.IsNullOrWhiteSpace(source.UserName)))
            return this.Error(this.Request.GetLanguage("SmtpTestNeeds") ?? "SMTP server and sender address are required.");

        try
        {
            var password = string.IsNullOrEmpty(smtpPassword)
                ? this.Config!.UnprotectSmtpPassword(saved.Password)
                : smtpPassword;
            var sender = new SmtpSender(source, password);
            await sender.SendAsync(to.Address,
                this.Request.GetLanguage("SmtpTestSubject") ?? "SMTP test mail",
                (this.Request.GetLanguage("SmtpTestBody") ?? "SMTP settings are working. ({0})").Formats(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC")!,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or NotSupportedException or System.Security.Cryptography.CryptographicException or IOException)
        {
            return this.Error((this.Request.GetLanguage("SmtpTestFailed") ?? "Failed to send the test mail: {0}").Formats(ex.GetBaseException().Message)!);
        }
        return this.Complete(message: (this.Request.GetLanguage("SmtpTestSent") ?? "Test mail sent to {0}.").Formats(to.Address));
    }

    /// <summary>
    /// Redis 캐시 구성을 조회합니다 (설치 결과 설정 파일 <see cref="SetupSettings.FileName"/>). 비밀번호는 내려주지 않고 저장 여부만 알려줍니다.
    /// </summary>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>redisConnection(비밀번호 제외)/redisHasPassword/redisDbIndex/redisInstanceName/redisRunning(지금 Redis 캐시 사용 중)</returns>
    [HttpGet("redis")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> GetRedis(CancellationToken cancellationToken = default)
    {
        var settings = await SetupSettings.LoadAsync(this.ContentRoot(), cancellationToken);
        var options = ParseRedis(settings["ConnectionStrings"]?["Redis"]?.ToString());
        return this.Complete(new
        {
            redisConnection = options?.ToString(includePassword: false) ?? string.Empty,
            redisHasPassword = !string.IsNullOrEmpty(options?.Password),
            redisDbIndex = settings["Cache"]?["DefaultDbIndex"]?.ToString() ?? string.Empty,
            redisInstanceName = settings["Cache"]?["InstanceName"]?.ToString() ?? string.Empty,
            redisRunning = CacheSelector.Redis?.IsAvailable == true
        });
    }

    /// <summary>
    /// Redis 캐시 구성을 설치 결과 설정 파일에 저장합니다. 캐시는 시작할 때 구성되므로 사이트를 다시 시작해야 적용됩니다.
    /// 접속 문자열을 비우면 Redis 구성을 지워 메모리 캐시를 씁니다. 비밀번호는 비워 두면 기존 값을 유지합니다.
    /// </summary>
    /// <param name="redisConnection">접속 문자열 (예: localhost:6379,ssl=false)</param>
    /// <param name="redisPassword">비밀번호 (비우면 기존 값 유지)</param>
    /// <param name="redisDbIndex">DB 번호 (비우면 서버 기본값)</param>
    /// <param name="redisInstanceName">키 접두사</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>처리 결과</returns>
    [HttpPut("redis")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveRedis([FromForm] string? redisConnection, [FromForm] string? redisPassword, [FromForm] int? redisDbIndex,
        [FromForm] string? redisInstanceName, CancellationToken cancellationToken = default)
    {
        if (redisDbIndex is < 0 or > 255) return this.Error(WebResources.ValidRegex.Formats(this.Lang("RedisDbIndex"))!);
        var root = this.ContentRoot();
        var settings = await SetupSettings.LoadAsync(root, cancellationToken);
        var connections = settings["ConnectionStrings"] as JsonObject ?? new JsonObject();
        settings["ConnectionStrings"] = connections;
        settings.Remove("Cache");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            var options = this.RedisOptions(redisConnection, redisPassword, connections["Redis"]?.ToString());
            if (options is null) return this.Error(WebResources.ValidRegex.Formats(this.Lang("SetupRedisConnection"))!);
            connections["Redis"] = options.ToString(includePassword: true);
            var cache = new JsonObject();
            if (redisDbIndex is not null) cache["DefaultDbIndex"] = redisDbIndex;
            if (redisInstanceName.Clip(64) is { Length: > 0 } instanceName) cache["InstanceName"] = instanceName;
            if (cache.Count > 0) settings["Cache"] = cache;
        }
        else connections.Remove("Redis");

        await SetupSettings.SaveAsync(root, settings, cancellationToken);
        return this.Complete(message: this.Lang("RestartToApply"));
    }

    /// <summary>
    /// 화면에 입력된 Redis 접속 문자열(저장 전 값 포함)로 접속해 PING 을 보냅니다. 비밀번호를 비워 두면 저장된 비밀번호를 씁니다. 설정은 저장하지 않습니다.
    /// </summary>
    /// <param name="redisConnection">접속 문자열</param>
    /// <param name="redisPassword">비밀번호 (비우면 저장된 값)</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>처리 결과 (응답 시간 안내 / 실패 사유)</returns>
    [HttpPost("redis/test")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> TestRedis([FromForm] string? redisConnection, [FromForm] string? redisPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(redisConnection)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("SetupRedisConnection"))!);
        var settings = await SetupSettings.LoadAsync(this.ContentRoot(), cancellationToken);
        var options = this.RedisOptions(redisConnection, redisPassword, settings["ConnectionStrings"]?["Redis"]?.ToString());
        if (options is null) return this.Error(WebResources.ValidRegex.Formats(this.Lang("SetupRedisConnection"))!);
        try
        {
            var elapsed = await RedisCacheProvider.PingAsync(options.ToString(includePassword: true));
            return this.Complete(message: this.Lang("SetupRedisOk").Formats(((int)elapsed.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)));
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or ArgumentException)
        {
            return this.Error(this.Lang("SetupTestFailed").Formats(ex.GetBaseException().Message)!);
        }
    }

    /// <summary>Redis 접속 문자열 해석 (비었거나 형식이 틀리면 null)</summary>
    private static ConfigurationOptions? ParseRedis(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection)) return null;
        try { return ConfigurationOptions.Parse(connection.Trim()); }
        catch (ArgumentException) { return null; }
    }

    /// <summary>화면 입력 접속 문자열 + 비밀번호(비었으면 문자열 안의 값, 그것도 없으면 저장된 값)로 접속 구성을 만듭니다 (형식이 틀리면 null).</summary>
    private ConfigurationOptions? RedisOptions(string connection, string? password, string? savedConnection)
    {
        var options = ParseRedis(connection);
        if (options is null) return null;
        if (!string.IsNullOrEmpty(password)) options.Password = password;
        else if (string.IsNullOrEmpty(options.Password)) options.Password = ParseRedis(savedConnection)?.Password;
        return options;
    }

    /// <summary>
    /// AI API 접속 정보 목록을 조회합니다 (설정 "AiConnections"). API 키는 내려주지 않고 저장 여부(hasApiKey)만 알려줍니다.
    /// </summary>
    /// <returns>접속 정보 목록</returns>
    [HttpGet("ai")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetAi() => this.Complete(AiConnection.ListOf(this.Config).Select(p => new
    {
        p.Id, p.Name, format = p.Format.ToString(), p.Endpoint, hasApiKey = !string.IsNullOrEmpty(p.ApiKey), p.Model,
        p.Temperature, p.TopP, p.TopK, p.MaxTokens, p.ReasoningEffort, p.Enabler, p.Active
    }));

    /// <summary>
    /// AI API 접속 하나를 등록하거나 수정합니다 (설정 "AiConnections" 목록 안에서 식별키로 교체, 없으면 추가).
    /// API 키는 비워 두면 기존 값을 유지하고, 입력하면 마스터 키의 용도 키로 암호화합니다. 기본 접속(active)으로 저장하면 다른 접속의 기본 표시는 꺼집니다.
    /// </summary>
    /// <param name="id">식별키 (비우면 새 접속)</param>
    /// <param name="name">표시 이름 (필수)</param>
    /// <param name="format">요청 형식 (OpenAI, Claude, Gemini)</param>
    /// <param name="endpoint">API 주소 (http/https, 필수)</param>
    /// <param name="apiKey">API 키 (비우면 기존 값 유지)</param>
    /// <param name="model">모델 이름 (필수)</param>
    /// <param name="temperature">온도 (0~2, 비우면 서버 기본값)</param>
    /// <param name="topP">Top P (0~1)</param>
    /// <param name="topK">Top K (1~1000)</param>
    /// <param name="maxTokens">최대 출력 토큰 (1~1,000,000)</param>
    /// <param name="reasoningEffort">추론 강도 (빈 값, low, medium, high)</param>
    /// <param name="enabler">사용 여부</param>
    /// <param name="active">기본 접속 여부</param>
    /// <returns>저장 결과 (data = 식별키)</returns>
    [HttpPut("ai")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveAi([FromForm] string? id, [FromForm] string? name, [FromForm] AiFormat format, [FromForm] string? endpoint, [FromForm] string? apiKey,
        [FromForm] string? model, [FromForm] double? temperature, [FromForm] double? topP, [FromForm] int? topK, [FromForm] int? maxTokens,
        [FromForm] string? reasoningEffort, [FromForm] bool enabler, [FromForm] bool active)
    {
        var list = AiConnection.ListOf(this.Config);
        var saved = list.FirstOrDefault(p => p.Id == id);
        var item = NewAi(saved?.Id ?? Guid.NewGuid().ToString("N"), name, format, endpoint, model, temperature, topP, topK, maxTokens, reasoningEffort);
        item.Enabler = enabler;
        item.Active = active;
        if (this.InvalidAi(item) is { } error) return this.Error(error);
        item.ApiKey = string.IsNullOrEmpty(apiKey) ? saved?.ApiKey : this.Config!.ProtectSecret(apiKey.Trim(), AiConnection.ProtectPurpose);
        if (saved is null && list.Count >= 20) return this.Error(this.Lang("MaxCountOf").Formats(this.Lang("AiConnection"), "20")!);

        if (item.Active) list.ForEach(p => p.Active = false);
        var index = saved is null ? -1 : list.IndexOf(saved);
        if (index < 0) list.Add(item);
        else list[index] = item;
        await this.Config!.SetModelAsync(this.DbContext!, AiConnection.Key, list);
        return this.Complete(item.Id, WebResources.SavedComplate);
    }

    /// <summary>
    /// 화면에 입력된 AI 접속 값(저장 전 포함)으로 짧은 질문을 보내 응답을 확인합니다. API 키를 비워 두면 저장된 키를 씁니다. 설정은 저장하지 않습니다.
    /// </summary>
    /// <param name="id">식별키 (저장된 API 키를 쓸 때)</param>
    /// <param name="name">표시 이름</param>
    /// <param name="format">요청 형식</param>
    /// <param name="endpoint">API 주소</param>
    /// <param name="apiKey">API 키 (비우면 저장된 값)</param>
    /// <param name="model">모델 이름</param>
    /// <param name="temperature">온도</param>
    /// <param name="topP">Top P</param>
    /// <param name="topK">Top K</param>
    /// <param name="maxTokens">최대 출력 토큰</param>
    /// <param name="reasoningEffort">추론 강도</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>성공 시 응답 글 앞부분을 담은 안내, 실패 시 사유</returns>
    [HttpPost("ai/test")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> TestAi([FromForm] string? id, [FromForm] string? name, [FromForm] AiFormat format, [FromForm] string? endpoint, [FromForm] string? apiKey,
        [FromForm] string? model, [FromForm] double? temperature, [FromForm] double? topP, [FromForm] int? topK, [FromForm] int? maxTokens,
        [FromForm] string? reasoningEffort, CancellationToken cancellationToken = default)
    {
        var item = NewAi(id ?? string.Empty, name, format, endpoint, model, temperature, topP, topK, maxTokens, reasoningEffort);
        if (this.InvalidAi(item) is { } error) return this.Error(error);
        try
        {
            var key = string.IsNullOrEmpty(apiKey)
                ? this.Config!.UnprotectSecret(AiConnection.ListOf(this.Config).FirstOrDefault(p => p.Id == id)?.ApiKey, AiConnection.ProtectPurpose)
                : apiKey.Trim();
            var result = await new AiClient(item, key).AskAsync(null, "Reply with OK.", cancellationToken);
            return result.Success
                ? this.Complete(message: this.Lang("AiTestOk").Formats(result.Data.Clip(200)))
                : this.Error(this.Lang("SetupTestFailed").Formats(result.Message)!);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(this.Lang("SetupTestFailed").Formats(ex.Message)!);
        }
    }

    /// <summary>화면 입력값으로 AI 접속 정보를 만듭니다 (값 정리 포함, 키·사용·기본 여부 제외).</summary>
    private static AiConnection NewAi(string id, string? name, AiFormat format, string? endpoint, string? model, double? temperature, double? topP, int? topK, int? maxTokens, string? reasoningEffort) => new()
    {
        Id = id,
        Name = name.Clip(64),
        Format = format,
        Endpoint = endpoint.Clip(512),
        Model = model.Clip(128),
        Temperature = temperature,
        TopP = topP,
        TopK = topK,
        MaxTokens = maxTokens,
        ReasoningEffort = reasoningEffort?.Trim().ToLowerInvariant() ?? string.Empty
    };

    /// <summary>
    /// AI API 접속 하나를 삭제합니다 (설정 "AiConnections" 목록에서 제거).
    /// </summary>
    /// <param name="id">식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("ai/{id}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> DeleteAi(string id)
    {
        var list = AiConnection.ListOf(this.Config);
        if (list.RemoveAll(p => p.Id == id) == 0) return this.NotFoundResult(WebResources.NotFoundData);
        await this.Config!.SetModelAsync(this.DbContext!, AiConnection.Key, list);
        return this.Complete(null, WebResources.DeletedComplete);
    }

    /// <summary>AI 접속 정보 하나를 검사합니다 (문제 없으면 null, 있으면 오류 메시지).</summary>
    private string? InvalidAi(AiConnection item)
    {
        if (item.Name.Length == 0) return WebResources.ValidRequired.Formats(this.Lang("Name"));
        if (item.Model.Length == 0) return WebResources.ValidRequired.Formats(this.Lang("AiModel"));
        if (!Enum.IsDefined(item.Format)) return WebResources.ValidRegex.Formats(this.Lang("AiFormat"));
        if (!Uri.TryCreate(item.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return WebResources.ValidRegex.Formats(this.Lang("ApiEndpoint"));
        if (item.Temperature is < 0 or > 2) return WebResources.ValidRegex.Formats(this.Lang("Temperature"));
        if (item.TopP is < 0 or > 1) return WebResources.ValidRegex.Formats("Top P");
        if (item.TopK is < 1 or > 1000) return WebResources.ValidRegex.Formats("Top K");
        if (item.MaxTokens is < 1 or > 1_000_000) return WebResources.ValidRegex.Formats(this.Lang("MaxTokens"));
        if (!AiConnection.ReasoningEfforts.Contains(item.ReasoningEffort)) return WebResources.ValidRegex.Formats(this.Lang("ReasoningEffort"));
        return null;
    }

    /// <summary>콘텐츠 루트 경로 (설치 결과 설정 파일 위치)</summary>
    private string ContentRoot() => this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
}
