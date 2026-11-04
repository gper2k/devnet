using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Data.CommonModel;
using BitPlus.Data.Content;
using BitPlus.Data.Provider;
using BitPlus.Data.SiteMap;
using BitPlus.Net;
using BitPlus.Security;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Site.Controllers.API;
using BitPlus.Web.Site.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BitPlus.Data.Cache;
using System.Globalization;
using System.Net.Mail;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 사이트 설치(Setup) 마법사 화면과 API 를 한 곳에서 처리하는 컨트롤러 (/Setup).
/// 설치 전에만 열리며(<see cref="Middlewares.SetupGuardExtend"/>), 입력값은 저장하지 않고 요청마다 전부 받아 처리합니다(상태 없음).
/// <para>설치 단계: database(DB 준비) → migrate(EF 마이그레이션) → seed(기본 데이터) → settings(SMTP 등 서비스 설정) → finish(설정 파일 기록 후 재시작)</para>
/// </summary>
public partial class SetupController : APIControllerBase
{
    /// <summary>설치 단계 이름 (화면 진행 순서)</summary>
    public static readonly string[] Steps = ["database", "migrate", "seed", "settings", "finish"];

    /// <summary>
    /// <see cref="SetupController"/> 기본 생성자.
    /// </summary>
    public SetupController() : base() { }

    /// <summary>
    /// 설치 마법사 화면.
    /// </summary>
    /// <returns>설치 마법사 뷰</returns>
    [HttpGet("/Setup")]
    public IActionResult Index() => this.View("~/Views/Setup/Index.cshtml");

    /// <summary>
    /// 마법사 초기 정보: 라이선스 본문(LICENSE.txt), 등록된 DB 프로바이더와 입력 항목, 기본 테이블 접두사(appsettings.json DbBuild:Prefix),
    /// 미리 채울 값(setup.json, 없으면 null), 설치 단계.
    /// </summary>
    /// <returns>license, providers, prefix, preset, steps</returns>
    [HttpGet("/Setup/Info")]
    public async Task<IActionResult> Info()
    {
        var root = this.ContentRoot();
        var presetPath = Path.Combine(root, SetupSettings.PresetFileName);
        return this.Complete(new
        {
            license = await this.ReadLicenseAsync(root),
            providers = DbProviders.All.Select(p => new
            {
                name = p.Name,
                title = p.Title,
                fields = p.Fields.Select(f => new { key = f.Key, label = this.Lang(f.LabelKey), type = f.Type, @default = f.Default, required = f.Required })
            }),
            prefix = this.HttpContext.RequestServices.GetRequiredService<IConfiguration>()[WebAppBuilderExtend.TablePrefixKey],
            preset = System.IO.File.Exists(presetPath) ? JsonNode.Parse(await System.IO.File.ReadAllTextAsync(presetPath)) : null,
            steps = Steps.Select(p => new { key = p, text = this.Lang("SetupStep" + char.ToUpperInvariant(p[0]) + p[1..]) })
        });
    }

    /// <summary>
    /// DB 접속 테스트. 선택한 프로바이더가 DB 종류에 맞는 조회를 ADO.NET 으로 직접 실행합니다(EF 미사용).
    /// </summary>
    /// <param name="provider">프로바이더 이름</param>
    /// <param name="values">입력값 JSON ({항목 키: 값})</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>version, databaseExists</returns>
    [HttpPost("/Setup/Test/Db")]
    public async Task<IActionResult> TestDb([FromForm] string? provider, [FromForm] string? values, CancellationToken cancellationToken = default)
    {
        var (db, connectionString, error) = this.ReadDb(provider, values);
        if (error is not null) return this.Error(error);
        try
        {
            var (version, exists) = await db!.TestAsync(connectionString!, cancellationToken);
            return this.Complete(new { version, databaseExists = exists },
                this.Lang("SetupTestOk").Formats(version) + " " + this.Lang(exists ? "SetupDbExists" : "SetupDbNew"));
        }
        catch (Exception ex)
        {
            return this.Error(this.Lang("SetupTestFailed").Formats(ex.GetBaseException().Message)!);
        }
    }

    /// <summary>
    /// SMTP 테스트 메일 발송 (설정은 저장하지 않음).
    /// </summary>
    /// <param name="smtpHost">SMTP 서버</param>
    /// <param name="smtpPort">포트</param>
    /// <param name="smtpSsl">SSL/TLS 사용</param>
    /// <param name="smtpUser">계정</param>
    /// <param name="smtpPassword">비밀번호</param>
    /// <param name="smtpFrom">보내는 주소</param>
    /// <param name="smtpFromName">보내는 이름</param>
    /// <param name="testTo">테스트 받을 주소</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>처리 결과</returns>
    [HttpPost("/Setup/Test/Smtp")]
    public async Task<IActionResult> TestSmtp([FromForm] string? smtpHost, [FromForm] int? smtpPort, [FromForm] bool smtpSsl, [FromForm] string? smtpUser,
        [FromForm] string? smtpPassword, [FromForm] string? smtpFrom, [FromForm] string? smtpFromName, [FromForm] string? testTo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(testTo) || !MailAddress.TryCreate(testTo.Trim(), out var to))
            return this.Error(WebResources.ValidEmail.Formats(this.Lang("TestMailTo"))!);

        var smtp = NewSmtp(smtpHost, smtpPort, smtpSsl, smtpUser, smtpFrom, smtpFromName);
        if (string.IsNullOrWhiteSpace(smtp.From) || string.IsNullOrWhiteSpace(smtp.Host))
            return this.Error(this.Lang("SmtpTestNeeds"));
        try
        {
            await new SmtpSender(smtp, smtpPassword).SendAsync(to.Address, this.Lang("SmtpTestSubject"),
                this.Lang("SmtpTestBody").Formats(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC")!, cancellationToken: cancellationToken);
            return this.Complete(message: this.Lang("SmtpTestSent").Formats(to.Address));
        }
        catch (Exception ex)
        {
            return this.Error(this.Lang("SetupTestFailed").Formats(ex.GetBaseException().Message)!);
        }
    }

    /// <summary>
    /// Redis 접속 테스트 (PING).
    /// </summary>
    /// <param name="redis">Redis 접속 문자열</param>
    /// <returns>처리 결과</returns>
    [HttpPost("/Setup/Test/Redis")]
    public async Task<IActionResult> TestRedis([FromForm] string? redis)
    {
        if (string.IsNullOrWhiteSpace(redis)) return this.Error(WebResources.ValidRequired.Formats(this.Lang("SetupRedisConnection"))!);
        try
        {
            var elapsed = await RedisCacheProvider.PingAsync(redis);
            return this.Complete(message: this.Lang("SetupRedisOk").Formats(((int)elapsed.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)));
        }
        catch (Exception ex)
        {
            return this.Error(this.Lang("SetupTestFailed").Formats(ex.GetBaseException().Message)!);
        }
    }

    /// <summary>
    /// 설치 단계 하나를 실행합니다. 같은 단계를 다시 실행해도 되도록(재시도) 만들어져 있습니다.
    /// </summary>
    /// <param name="step">단계 이름 (<see cref="Steps"/>)</param>
    /// <param name="provider">DB 프로바이더 이름</param>
    /// <param name="values">DB 입력값 JSON ({항목 키: 값})</param>
    /// <param name="prefix">테이블 이름 접두사 (영문으로 시작하는 영문·숫자 16자 이내, 비우면 접두사 없음)</param>
    /// <param name="smtpHost">SMTP 서버 (없으면 SMTP 건너뜀)</param>
    /// <param name="smtpPort">SMTP 포트</param>
    /// <param name="smtpSsl">SMTP SSL/TLS 사용</param>
    /// <param name="smtpUser">SMTP 계정</param>
    /// <param name="smtpPassword">SMTP 비밀번호</param>
    /// <param name="smtpFrom">보내는 주소</param>
    /// <param name="smtpFromName">보내는 이름</param>
    /// <param name="redis">Redis 접속 문자열 (없으면 건너뜀)</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>단계 결과 메시지</returns>
    [HttpPost("/Setup/Install/{step}")]
    public async Task<IActionResult> Install(string step, [FromForm] string? provider, [FromForm] string? values, [FromForm] string? prefix,
        [FromForm] string? smtpHost, [FromForm] int? smtpPort, [FromForm] bool smtpSsl, [FromForm] string? smtpUser, [FromForm] string? smtpPassword,
        [FromForm] string? smtpFrom, [FromForm] string? smtpFromName, [FromForm] string? redis, CancellationToken cancellationToken = default)
    {
        var (db, connectionString, error) = this.ReadDb(provider, values);
        if (error is not null) return this.Error(error);
        prefix = prefix?.Trim();
        if (!string.IsNullOrEmpty(prefix) && !PrefixRegex().IsMatch(prefix)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("DbTablePrefix"))!);
        try
        {
            switch (step)
            {
                case "database":
                    var created = await db!.EnsureDatabaseAsync(connectionString!, cancellationToken);
                    return this.Complete(message: this.Lang(created ? "SetupDbCreated" : "SetupDbReady"));
                case "migrate":
                    var progress = SetupMigrationProgress.Instance.Snapshot;
                    if (progress.Running)
                        return this.Complete(new { running = true, current = progress.Current, total = progress.Total });

                    if (progress.Succeeded)
                        return this.Complete(message: progress.Message);

                    int totalOps = 0;
                    await using (var context = this.CreateContext(db!, connectionString!, prefix))
                    {
                        var assembly = context.GetService<IMigrationsAssembly>();
                        foreach (var id in await context.Database.GetPendingMigrationsAsync(cancellationToken))
                            totalOps += assembly.CreateMigration(assembly.Migrations[id], context.Database.ProviderName!).UpOperations.Count;

                        if (totalOps == 0)
                        {
                            SetupMigrationProgress.Instance.Finish(true, message: this.Lang("SetupDbReady"));
                            return this.Complete(new { running = false, succeeded = true, message = this.Lang("SetupDbReady") });
                        }

                        SetupMigrationProgress.Instance.Start(totalOps);
                    }

                    var scopeFactory = this.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = scopeFactory.CreateScope();
                            await using var context = this.CreateContext(db!, connectionString!, prefix, scope.ServiceProvider);
                            context.Database.SetCommandTimeout(600);
                            await context.Database.MigrateAsync(CancellationToken.None);
                            var applied = await context.Database.GetAppliedMigrationsAsync(CancellationToken.None);
                            SetupMigrationProgress.Instance.Finish(true, message: string.Join(", ", applied));
                        }
                        catch (Exception ex)
                        {
                            this.Logger?.LogError(ex, "Setup migration failed.");
                            SetupMigrationProgress.Instance.Finish(false, error: ex.GetBaseException().Message);
                        }
                    });
                    return this.Complete(new { running = true, current = 0, total = totalOps });
                case "seed":
                    await using (var context = this.CreateContext(db!, connectionString!, prefix))
                    {
                        context.Database.SetCommandTimeout(600);
                        await this.SeedAsync(context, cancellationToken);
                    }
                    return this.Complete();
                case "settings":
                    if (string.IsNullOrWhiteSpace(smtpHost)) return this.Complete(message: this.Lang("SetupSkipped"));
                    await using (var context = this.CreateContext(db!, connectionString!, prefix))
                        await this.SaveSmtpAsync(context, NewSmtp(smtpHost, smtpPort, smtpSsl, smtpUser, smtpFrom, smtpFromName), smtpPassword, cancellationToken);
                    return this.Complete();
                case "finish":
                    await this.FinishAsync(db!, connectionString!, prefix, redis, cancellationToken);
                    return this.Complete(message: this.Lang("SetupDone"));
                default:
                    return this.NotFoundResult();
            }
        }
        catch (Exception ex)
        {
            this.Logger?.LogError(ex, "Setup step {Step} failed.", step);
            return this.Error(ex.GetBaseException().Message);
        }
    }

    /// <summary>
    /// 마이그레이션 진행 상태 (설치 화면이 migrate 단계 동안 주기적으로 조회).
    /// </summary>
    /// <returns>진행 상태 (running, current, total, succeeded, message, error)</returns>
    [HttpGet("/Setup/Progress")]
    public IActionResult Progress()
    {
        var (running, current, total, succeeded, message, error) = SetupMigrationProgress.Instance.Snapshot;
        return this.Complete(new { running, current, total, succeeded, message, error });
    }

    /// <summary>
    /// 프로바이더와 입력값을 검사해 접속 문자열을 만듭니다.
    /// </summary>
    /// <returns>프로바이더, 접속 문자열, 오류 메시지 (정상이면 null)</returns>
    private (IDbProvider? Provider, string? ConnectionString, string? Error) ReadDb(string? provider, string? values)
    {
        var db = DbProviders.Get(provider);
        if (db is null) return (null, null, WebResources.ValidRequired.Formats(this.Lang("SetupDbProvider")));

        var map = ParseMap(values);
        var missing = db.Fields.FirstOrDefault(p => p.Required && string.IsNullOrWhiteSpace(map.GetValueOrDefault(p.Key)));
        if (missing is not null) return (db, null, WebResources.ValidRequired.Formats(this.Lang(missing.LabelKey)));
        return (db, db.BuildConnectionString(map), null);
    }

    /// <summary>
    /// 설치용 DbContext 를 만듭니다 (DI 에는 설치 전 DB 가 연결되어 있지 않으므로 입력값으로 직접 구성, 앱과 같은 확장 옵션 적용).
    /// </summary>
    private SiteDbContext CreateContext(IDbProvider provider, string connectionString, string? prefix)
        => this.CreateContext(provider, connectionString, prefix, this.HttpContext.RequestServices);

    /// <summary>
    /// 지정한 IServiceProvider 를 사용해 설치용 DbContext 를 만듭니다 (백그라운드 비동기 태스크 지원).
    /// </summary>
    private SiteDbContext CreateContext(IDbProvider provider, string connectionString, string? prefix, IServiceProvider services)
    {
        var options = new DbContextOptionsBuilder<SiteDbContext>();
        WebAppBuilderExtend.BindDbOption(services, options, o =>
        {
            provider.UseProvider(o, connectionString, prefix);
            // 마이그레이션은 접두사 토큰으로 만들어져 실제 모델과 이름이 달라 보이므로, 모델 변경 경고(마이그레이션 중단)를 끈다
            o.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            o.AddInterceptors(SetupMigrationProgress.Instance); // 마이그레이션 진행(실행한 CREATE/ALTER 문 수) 집계
        }, prefix ?? string.Empty);
        return new SiteDbContext(options.Options);
    }

    /// <summary>
    /// 기본 데이터 등록: 시스템 역할(SystemManager 최종 관리자, Manager 일반 관리자, AccountManager 계정 관리자, ContentManager 컨텐츠 관리자, Member 일반), 시스템 그룹(Manager = Manager 역할 연결, 그 아래 AccountManager = AccountManager 역할 연결·ContentManager = ContentManager 역할 연결, Member), 가입 기본 역할·그룹(Member),
    /// 관리자 시스템 메뉴(영문 이름)와 사용자 시스템 메뉴 "User"(하위 Home·View(/view/1 환영 페이지)·Content), 컨텐츠(게시글) 그룹 루트 시스템 메뉴 "ContentGroup"(표시명 "게시 그룹"), 보기 전용 컨텐츠 규칙 "PageView"(첫 글: wwwroot/html/Welcome.ko.html 원문 + Welcome.en.html 영문 번역, 제목 WelcomePage), 하단 메뉴 "Footer"(이용약관·개인정보 처리방침 — html/Terms·Privacy 의 .ko.html 원문 + .en.html 영문 번역 보기 전용 페이지), 기본 게시판·샘플 글·사용자 메뉴·홈/포탈 배치(<see cref="SiteSeed"/>, Data/SiteSeed.json), 인증·암호화 마스터 키. 이미 있는 항목은 건너뜁니다.
    /// </summary>
    private async Task SeedAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        var systemManager = await this.EnsureRoleAsync(db, SystemNames.SystemManagerRole, "RoleSystemManagerName", cancellationToken);
        var manager = await this.EnsureRoleAsync(db, SystemNames.ManagerRole, "RoleManagerName", cancellationToken);
        var accountManager = await this.EnsureRoleAsync(db, SystemNames.AccountManagerRole, "RoleAccountManagerName", cancellationToken);
        var contentManager = await this.EnsureRoleAsync(db, SystemNames.ContentManagerRole, "RoleContentManagerName", cancellationToken);
        var member = await this.EnsureRoleAsync(db, SystemNames.MemberRole, "RoleMemberName", cancellationToken);
        var group = await this.EnsureGroupAsync(db, SystemNames.MemberGroup, "RoleMemberName", null, null, cancellationToken);
        var managerGroup = await this.EnsureGroupAsync(db, SystemNames.ManagerGroup, "RoleManagerName", manager, null, cancellationToken);
        await this.EnsureGroupAsync(db, SystemNames.AccountManagerGroup, "RoleAccountManagerName", accountManager, managerGroup, cancellationToken);
        await this.EnsureGroupAsync(db, SystemNames.ContentManagerGroup, "RoleContentManagerName", contentManager, managerGroup, cancellationToken);
        await this.EnsureAdminMenusAsync(db, [systemManager, manager], cancellationToken);
        await this.EnsureUserMenuAsync(db, cancellationToken);
        await this.EnsureContentMapMenuAsync(db, cancellationToken);
        await this.EnsurePageViewRuleAsync(db, cancellationToken);
        await this.EnsureWelcomePageAsync(db, cancellationToken);
        await this.EnsureFooterMenuAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await LanguageSeed.SyncAsync(db, this.ContentRoot(), cancellationToken);
        // 기본 게시판·샘플 글·사용자 메뉴·홈/포탈 배치 (Data/SiteSeed.json, 시스템 메뉴가 저장된 뒤)
        var roles = new Dictionary<string, Guid> { [systemManager.Name!] = systemManager.Id, [manager.Name!] = manager.Id, [accountManager.Name!] = accountManager.Id, [contentManager.Name!] = contentManager.Id, [member.Name!] = member.Id };
        await SiteSeed.ApplyAsync(db, this.Config!, this.ContentRoot(), roles, cancellationToken);

        var config = this.Config!;
        await config.SetModelAsync(db, MembershipController.AccountRuleKey,
            new AccountRule { DefaultRoles = [member.Id], DefaultRoleId = member.Id, DefaultGroups = [group.Id], DefaultGroupId = group.Id }, cancellationToken: cancellationToken);
        if (!await db.Settings.AnyAsync(p => p.Category == config.Category && p.Key == WebConfig.SecretKeyName, cancellationToken))
            await config.SetAsync(db, WebConfig.SecretKeyName, KeyGenerator.GenerateAesKey(), WebResources.SecretKeyDescription, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 보기 전용(OnlyView) 컨텐츠 규칙을 만듭니다: 제목 "PageView". 보기 전용 규칙은 사이트에 하나만 두므로 이미 있으면 건너뜁니다.
    /// </summary>
    private async Task EnsurePageViewRuleAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        if (await db.ContentRules.AnyAsync(p => p.OnlyView, cancellationToken)) return;

        db.ContentRules.Add(await this.NewContentRuleAsync(db, "PageView", true, cancellationToken));
    }

    /// <summary>환영 페이지(보기 전용 규칙의 첫 글)의 글 번호 — User 메뉴의 View 가 이 글의 보기 경로(/view/{번호})를 가리킴</summary>
    private const int WelcomeContentNo = 1;

    /// <summary>
    /// 보기 전용(PageView) 규칙의 첫 글로 환영 페이지를 등록합니다: 제목 <c>WelcomePage</c>(보기 화면의 페이지 제목), 본문 = 웹 루트의 <c>html/Welcome.ko.html</c>(원문 한국어) + <c>Welcome.en.html</c>(영문 번역).
    /// 보기 전용 규칙에 이미 글이 있거나 원문 파일이 없으면 건너뜁니다 (설치는 계속).
    /// </summary>
    private async Task EnsureWelcomePageAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        var rule = db.ContentRules.Local.FirstOrDefault(p => p.OnlyView) ?? await db.ContentRules.FirstOrDefaultAsync(p => p.OnlyView, cancellationToken);
        if (rule is null || await db.Contents.AnyAsync(p => p.ContentRuleId == rule.Id, cancellationToken)) return;

        if (await this.NewPageAsync(rule.Id, WelcomeContentNo, "WelcomePage", "Welcome", cancellationToken) is { } page) db.Contents.Add(page);
    }

    /// <summary>
    /// 기본 보기 전용 페이지를 만듭니다: 원문은 웹 루트 <c>html/{이름}.ko.html</c>(원문 언어 ko, 제목은 리소스의 한국어), <c>{이름}.en.html</c> 이 있으면 영문 번역(사람 번역, 제목은 리소스의 영어)을 함께 붙입니다.
    /// 영문 번역은 지원 문화권과 상관없이 넣습니다 (지원 문화권에 en 이 없으면 보이지 않다가 en 을 추가하면 바로 보임). 원문 파일이 없으면 null 입니다.
    /// </summary>
    /// <param name="ruleId">보기 전용 규칙 식별키</param>
    /// <param name="no">글 번호</param>
    /// <param name="titleKey">제목 리소스 키</param>
    /// <param name="name">html 파일 이름 (문화권·확장자 제외)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task<ContentModel?> NewPageAsync(Guid ruleId, int no, string titleKey, string name, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath, "html");
        var original = Path.Combine(folder, $"{name}.ko.html");
        if (!System.IO.File.Exists(original)) return null;

        var page = new ContentModel
        {
            Title = this.TextOf(titleKey, "ko"),
            ContentBody = await System.IO.File.ReadAllTextAsync(original, cancellationToken),
            Culture = "ko",
            ContentRuleId = ruleId,
            ContentNo = no,
            TargetNo = no,
            LevelNo = 0,
            SortNo = 0,
            Approved = true,
            Visible = true,
            Published = true
        };
        var english = Path.Combine(folder, $"{name}.en.html");
        if (System.IO.File.Exists(english))
            page.Translations.Add(new ContentTranslation { Culture = "en", Title = this.TextOf(titleKey, "en"), Body = await System.IO.File.ReadAllTextAsync(english, cancellationToken), SourceUDate = page.UDate });
        return page;
    }

    /// <summary>
    /// 하단 메뉴 "Footer"(표시명 "하단 메뉴", 하위 메뉴를 묶는 메뉴 그룹, 접근 제한 없음)와 그 아래 이용약관·개인정보 처리방침 메뉴를 등록합니다.
    /// 하위 메뉴는 일반 메뉴(시스템 아님)라 관리자가 수정·삭제할 수 있습니다. 메뉴가 없을 때마다 보기 전용(PageView) 규칙에 웹 루트의 <c>html/Terms.ko.html</c>·<c>Privacy.ko.html</c>(원문 한국어)과 <c>.en.html</c>(영문 번역)로 페이지를 만들고(<see cref="NewPageAsync"/>), 메뉴가 그 보기 경로(/view/{번호})를 가리키게 합니다.
    /// 이미 있는 메뉴와 파일이 없는 항목은 건너뜁니다 (설치는 계속). 약관 본문의 [사이트명]·[시행일] 등은 관리자가 페이지 컨텐츠에서 채웁니다.
    /// </summary>
    private async Task EnsureFooterMenuAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        var root = await db.Menus.FirstOrDefaultAsync(p => p.Key == SystemNames.FooterMenu, cancellationToken);
        if (root is null)
        {
            root = this.NewMenu(SystemNames.FooterMenu, this.TextOf("FooterMenuName", "en"), null, "fiLink", null, 4, null, LinkType.None);
            root.DpNames.Add(new DpName<AppMenu, string>(root.Key, "ko", this.TextOf("FooterMenuName", "ko")));
            db.Menus.Add(root);
        }
        var rule = db.ContentRules.Local.FirstOrDefault(p => p.OnlyView) ?? await db.ContentRules.FirstOrDefaultAsync(p => p.OnlyView, cancellationToken);
        if (rule is null) return;

        (string Key, string NameKey, string File, string Icon)[] items =
        [
            ("FooterTerms", "TermsOfService", "Terms", "fiDocText"),
            ("FooterPrivacy", "PrivacyPolicy", "Privacy", "fiShield"),
        ];
        // 다음 글 번호: 같은 설치 배치에서 추가한 글(환영 페이지)까지 포함한 보기 전용 규칙의 최대 번호 + 1
        var next = Math.Max(db.Contents.Local.Where(p => p.ContentRuleId == rule.Id).Max(p => p.ContentNo) ?? 0,
            await db.Contents.Where(p => p.ContentRuleId == rule.Id).MaxAsync(p => p.ContentNo, cancellationToken) ?? 0);
        for (var i = 0; i < items.Length; i++)
        {
            if (await db.Menus.AnyAsync(p => p.Key == items[i].Key, cancellationToken) || await this.NewPageAsync(rule.Id, next + 1, items[i].NameKey, items[i].File, cancellationToken) is not { } page) continue;
            next++;
            db.Contents.Add(page);
            var menu = this.NewMenu(items[i].Key, this.TextOf(items[i].NameKey, "en"), $"/view/{next}", items[i].Icon, root, i + 1, null, isSystem: false);
            menu.DpNames.Add(new DpName<AppMenu, string>(menu.Key, "ko", this.TextOf(items[i].NameKey, "ko")));
            db.Menus.Add(menu);
        }
    }

    /// <summary>
    /// 컨텐츠 규칙 하나를 만듭니다 (제목은 모든 문화권 같은 이름, 규칙 번호는 <see cref="SiteSeed.NewRuleAsync"/> 규칙).
    /// </summary>
    private Task<ContentRule> NewContentRuleAsync(SiteDbContext db, string title, bool onlyView, CancellationToken cancellationToken)
        => SiteSeed.NewRuleAsync(db, new[] { "ko", "en" }.Select(culture => KeyValuePair.Create(culture, title)), onlyView, cancellationToken);

    /// <summary>
    /// 시스템 역할을 찾고 없으면 추가합니다 (다국어 표시명 = 리소스 ko/en).
    /// </summary>
    /// <returns>역할</returns>
    private async Task<AppRole> EnsureRoleAsync(SiteDbContext db, string name, string nameKey, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();
        var role = await db.Roles.FirstOrDefaultAsync(p => p.NormalizedName == normalized, cancellationToken);
        if (role is not null) return role;

        role = new AppRole(name, visible: true, isSystem: true) { Description = this.Lang(nameKey) };
        foreach (var culture in new[] { "ko", "en" })
            role.DpNames.Add(new DpName<AppRole, Guid>(role.Id, culture, this.Request.GetLanguage(nameKey, CultureInfo.GetCultureInfo(culture)) ?? name));
        db.Roles.Add(role);
        return role;
    }

    /// <summary>
    /// 시스템 그룹을 찾고 없으면 추가합니다 (parent 가 없으면 최상위, 있으면 그 아래 그룹, 다국어 표시명 = 리소스 ko/en). role 이 있으면 그 역할을 그룹 역할로 연결합니다 (그룹 소속 사용자가 역할을 얻음).
    /// 그룹 경로(Location)는 그룹 관리(CreateGroupAsync)와 같은 규칙으로 정합니다: 최상위 "/", 하위 "{상위 경로}{상위 식별키}/".
    /// </summary>
    /// <returns>그룹</returns>
    private async Task<AppGroup> EnsureGroupAsync(SiteDbContext db, string name, string nameKey, AppRole? role, AppGroup? parent, CancellationToken cancellationToken)
    {
        var parentId = parent?.Id;
        var group = await db.Groups.FirstOrDefaultAsync(p => p.Name == name && p.ParentId == parentId, cancellationToken);
        if (group is not null)
        {
            if (role is not null && !await db.GroupRoles.AnyAsync(p => p.GroupId == group.Id && p.RoleId == role.Id, cancellationToken))
                group.GroupRoles.Add(new AppGroupRole(group.Id, role.Id, true));
            return group;
        }

        group = new AppGroup(name, parentId: parentId, isSystem: true) { Description = this.Lang(nameKey), Location = parent is null ? "/" : $"{parent.Location}{parent.Id}/" };
        if (role is not null) group.GroupRoles.Add(new AppGroupRole(group.Id, role.Id, true));
        foreach (var culture in new[] { "ko", "en" })
            group.DpNames.Add(new DpName<AppGroup, Guid>(group.Id, culture, this.Request.GetLanguage(nameKey, CultureInfo.GetCultureInfo(culture)) ?? name));
        db.Groups.Add(group);
        return group;
    }

    /// <summary>
    /// 관리자 콘솔 시스템 메뉴를 등록합니다 (삭제 불가, 이름은 영문 — 최상위만 한글 표시명 "관리자", 접근은 관리자 정책과 같은 최종 관리자(SystemManager)·일반 관리자(Manager) 역할). 이미 있는 메뉴는 건너뜁니다.
    /// 최상위 "Manager"(Admin Console) 아래에 사이드바와 같은 순서로 Dashboard, Account, Content, Services, External Services, System Settings, UI Dev Kit 을 둡니다.
    /// </summary>
    private async Task EnsureAdminMenusAsync(SiteDbContext db, AppRole[] roles, CancellationToken cancellationToken)
    {
        var root = await db.Menus.FirstOrDefaultAsync(p => p.Key == "Manager", cancellationToken);
        if (root is null)
        {
            root = this.NewMenu("Manager", this.TextOf("AdminMenuName", "en"), "/Manager", "fiSettings", null, 1, roles);
            root.DpNames.Add(new DpName<AppMenu, string>(root.Key, "ko", this.TextOf("AdminMenuName", "ko")));
            db.Menus.Add(root);
        }
        (string Key, string Name, string Action, string Icon)[] items =
        [
            ("ManagerDashboard", "Dashboard", "/Manager", "fiBoard"),
            ("ManagerAccount", "Account", "/Manager/Account", "fiPerson"),
            ("ManagerContent", "Content", "/Manager/Content", "fiDocOnePage"),
            ("ManagerService", "Services", "/Manager/Service", "fiFlashOn"),
            ("ManagerExternal", "External Services", "/Manager/External", "fiCloud"),
            ("ManagerSystem", "System Settings", "/Manager/System", "fiSettings"),
            ("ManagerDevKit", "UI Dev Kit", "/Manager/DevKit", "fiApps"),
        ];
        for (var i = 0; i < items.Length; i++)
            if (!await db.Menus.AnyAsync(p => p.Key == items[i].Key, cancellationToken))
                db.Menus.Add(this.NewMenu(items[i].Key, items[i].Name, items[i].Action, items[i].Icon, root, i + 1, roles));

        var config = this.Config!;
        if (!await db.Settings.AnyAsync(p => p.Category == config.Category && p.Key == "AdminMenu", cancellationToken))
            await config.SetAsync(db.Settings, "AdminMenu", root.Key, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 사용자(사이트 이용자)용 최상위 시스템 메뉴 "User"(영문 이름, 한글 표시명 "사용자", 삭제 불가)와 기본 하위 메뉴 Home(/), View(/view/1 — 환영 페이지), Content(/content)를 등록합니다.
    /// "User" 는 하위 메뉴를 묶는 메뉴 그룹이고 접근 제한이 없습니다. 하위 메뉴는 일반 메뉴(시스템 아님)라 관리자가 수정·삭제할 수 있습니다. 이미 있는 메뉴는 건너뜁니다.
    /// </summary>
    private async Task EnsureUserMenuAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        var root = await db.Menus.FirstOrDefaultAsync(p => p.Key == "User", cancellationToken);
        if (root is null)
        {
            root = this.NewMenu("User", this.TextOf("User", "en"), null, "fiPerson", null, 2, null, LinkType.None);
            root.DpNames.Add(new DpName<AppMenu, string>(root.Key, "ko", this.TextOf("User", "ko")));
            db.Menus.Add(root);
        }
        (string Key, string Name, string Action, string Icon)[] items =
        [
            ("UserHome", "Home", "/", "fiHome"),
            ("UserView", "View", $"/view/{WelcomeContentNo}", "fiEdit"),
            ("UserContent", "Content", "/content", "fiDocOnePage"),
        ];
        for (var i = 0; i < items.Length; i++)
            if (!await db.Menus.AnyAsync(p => p.Key == items[i].Key, cancellationToken))
                db.Menus.Add(this.NewMenu(items[i].Key, items[i].Name, items[i].Action, items[i].Icon, root, i + 1, null, isSystem: false));
    }

    /// <summary>
    /// 컨텐츠(게시글)를 그룹으로 묶어 메뉴를 자동 구성하기 위한 최상위 시스템 메뉴 "ContentMap"(삭제 불가, 영문 이름 "Content Map", 한글 표시명 "컨텐츠 맵")을 등록합니다.
    /// 하위 메뉴를 묶는 메뉴 그룹이고 접근 제한이 없습니다. 이미 있으면 건너뜁니다.
    /// </summary>
    private async Task EnsureContentMapMenuAsync(SiteDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Menus.AnyAsync(p => p.Key == "ContentMap", cancellationToken)) return;
        var menu = this.NewMenu("ContentMap", this.TextOf("ContentMapName", "en"), null, "fiFolder", null, 3, null, LinkType.None);
        menu.DpNames.Add(new DpName<AppMenu, string>(menu.Key, "ko", this.TextOf("ContentMapName", "ko")));
        db.Menus.Add(menu);
    }

    /// <summary>
    /// 설치 메뉴 하나를 만듭니다 (영문 이름, 아이콘 태그, 기본은 시스템 메뉴). roles 가 있으면 그 역할들만 접근하고, 없으면 접근 제한이 없습니다.
    /// </summary>
    private AppMenu NewMenu(string key, string name, string? action, string icon, AppMenu? parent, int sortOrder, AppRole[]? roles, LinkType type = LinkType.Move, bool isSystem = true)
    {
        var menu = new AppMenu(key, type, action, $"<i class=\"fi {icon} fiFill\"></i>", parent, sortOrder, isSystem);
        menu.DpNames.Add(new DpName<AppMenu, string>(key, "en", name));
        foreach (var role in roles ?? []) menu.Accesses.Add(new AppMenuAccess(key, AccountTypes.Role, role.Id));
        return menu;
    }

    /// <summary>
    /// 리소스 문구를 지정 문화권으로 조회합니다 (설치 시드의 다국어 표시명용, 없으면 키).
    /// </summary>
    private string TextOf(string key, string culture) => this.Request.GetLanguage(key, CultureInfo.GetCultureInfo(culture)) ?? key;

    /// <summary>
    /// SMTP 구성을 저장합니다 (비밀번호는 설치 DB 의 마스터 키로 암호화).
    /// </summary>
    private async Task SaveSmtpAsync(SiteDbContext db, SmtpConfig smtp, string? password, CancellationToken cancellationToken)
    {
        var config = this.Config!;
        var key = await db.Settings.Where(p => p.Category == config.Category && p.Key == WebConfig.SecretKeyName).Select(p => p.Value).FirstAsync(cancellationToken);
        config.UseSecretKey(key!);
        smtp.Password = config.ProtectSmtpPassword(password);
        await config.AccountConfig.SetModelAsync(db, WebConfig.AccountSmtpKey, smtp, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 설치 결과 설정 파일(<see cref="SetupSettings.FileName"/>)을 쓰고, 응답을 보낸 뒤 앱을 종료해 새 설정으로 다시 시작되게 합니다.
    /// </summary>
    private async Task FinishAsync(IDbProvider provider, string connectionString, string? prefix, string? redis, CancellationToken cancellationToken)
    {
        var connections = new JsonObject { ["DefaultConnection"] = connectionString };
        if (!string.IsNullOrWhiteSpace(redis)) connections["Redis"] = redis.Trim();
        var settings = new JsonObject
        {
            [SetupSettings.CreatedKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["Database"] = new JsonObject { ["Provider"] = provider.Name },
            ["DbBuild"] = new JsonObject { ["Prefix"] = prefix ?? string.Empty },
            ["ConnectionStrings"] = connections
        };
        await SetupSettings.SaveAsync(this.ContentRoot(), settings, cancellationToken);
        if (!string.IsNullOrWhiteSpace(redis)) await ClearSiteCacheAsync(redis.Trim());

        var lifetime = this.HttpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();
        this.Response.OnCompleted(() =>
        {
            _ = Task.Delay(500).ContinueWith(_ => lifetime.StopApplication(), TaskScheduler.Default);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// 다시 설치할 때 Redis 에 남아 있는 이전 설치의 사이트 캐시(설정·다국어·계정·정책 등)를 지웁니다.
    /// DB 는 새로 만들어도 Redis 는 그대로라, 설정 모델처럼 만료 없이 캐시된 값(예: 이전 마스터 키로 암호화된 SMTP 비밀번호)이 새 DB 값보다 먼저 읽히기 때문입니다.
    /// </summary>
    /// <param name="redis">Redis 접속 문자열</param>
    private static async Task ClearSiteCacheAsync(string redis)
    {
        // ponytail: 이 사이트가 쓰는 캐시 키 접두사 목록 — 새 캐시 키 접두사를 만들면 여기에 더할 것. Redis 키에 사이트 구분 접두사(InstanceName)를 두면 필요 없어짐
        string[] prefixes = ["AppConfig:", "Lang.", "account", "policy:", "popup:", "uid:", "user", "template:", "ga:", "content.", "widget."];
        var cache = new RedisCacheProvider(redis);
        foreach (var prefix in prefixes) await cache.RemoveByPrefixAsync(prefix);
    }

    /// <summary>
    /// 화면 입력값으로 SMTP 구성을 만듭니다 (비밀번호 제외).
    /// </summary>
    private static SmtpConfig NewSmtp(string? host, int? port, bool ssl, string? user, string? from, string? fromName) => new()
    {
        Host = host?.Trim(),
        Port = port ?? SmtpSender.StartTlsPort,
        UseSsl = ssl,
        UserName = user?.Trim(),
        From = from?.Trim(),
        FromName = fromName?.Trim()
    };

    /// <summary>테이블 이름 접두사 형식: 영문으로 시작하는 영문·숫자 16자 이내</summary>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z][A-Za-z0-9]{0,15}$")]
    private static partial System.Text.RegularExpressions.Regex PrefixRegex();

    /// <summary>
    /// 콘텐츠 루트 경로 (LICENSE.txt, setup.json, 설치 결과 설정 파일 위치).
    /// </summary>
    private string ContentRoot() => this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

    /// <summary>
    /// 라이선스 텍스트 조회: ContentRoot, AppContext.BaseDirectory, 상위 디렉터리 순으로 LICENSE.txt 탐색 후 없으면 기본 MIT 라이선스 반환.
    /// </summary>
    private async Task<string> ReadLicenseAsync(string root)
    {
        var candidates = new[]
        {
            Path.Combine(root, "LICENSE.txt"),
            Path.Combine(AppContext.BaseDirectory, "LICENSE.txt"),
            Path.Combine(root, "..", "LICENSE.txt")
        };
        foreach (var path in candidates)
            if (System.IO.File.Exists(path)) return await System.IO.File.ReadAllTextAsync(path);
        return DefaultLicense;
    }

    private const string DefaultLicense = """
        MIT License

        Copyright (c) 2026 BitPlus (비트플러스)

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.

        USER RESPONSIBILITY

        The user installs, configures, operates and uses the Software entirely at the
        user's own risk. The user is solely responsible for any damage or loss arising
        from the use of the Software, including but not limited to data loss, service
        interruption, security incidents, business loss, and damage to third parties.
        The user is responsible for backing up data, verifying security settings and
        complying with applicable laws before and while using the Software.

        ----------------------------------------------------------------------------

        [한국어 안내 - 참고용, 영문 원문이 우선합니다]

        MIT 라이선스

        이 소프트웨어는 무료로 사용, 복제, 수정, 병합, 게시, 배포, 재라이선스 및
        판매할 수 있습니다. 단, 위 저작권 표시와 이 허가 표시를 소프트웨어의 모든
        복제본 또는 주요 부분에 포함해야 합니다.

        이 소프트웨어는 "있는 그대로" 제공되며, 상품성, 특정 목적 적합성, 비침해에
        대한 보증을 포함하여 명시적이거나 묵시적인 어떠한 보증도 하지 않습니다.
        저작자 또는 저작권자는 소프트웨어나 그 사용으로 인해 발생하는 어떠한 청구,
        손해 또는 기타 책임에 대해서도 책임지지 않습니다.

        사용자 책임

        사용자는 자신의 책임하에 이 소프트웨어를 설치, 설정, 운영 및 사용합니다.
        데이터 손실, 서비스 중단, 보안 사고, 영업 손실, 제3자에 대한 피해 등 이
        소프트웨어의 사용으로 발생하는 모든 피해와 손실에 대한 책임은 전적으로
        사용자에게 있습니다. 사용자는 사용 전과 사용 중에 데이터 백업, 보안 설정
        확인 및 관련 법령 준수에 대한 책임을 집니다.
        """;
}
