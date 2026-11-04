using BitPlus.Data.Provider;
using BitPlus.Web;
using BitPlus.Web.Site.Data;
using BitPlus.Web.Site.Middlewares;
using BitPlus.Web.Site.OAuth;
using BitPlus.Web.Site.Plugins;
using BitPlus.Web.Site.Widgets;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 0. 설치 결과 설정(appsettings.Setup.json) 추가 — 설치 여부는 "Created" 값만으로 판단한다 (DB·네트워크를 보지 않으므로 운영 중 장애로 설치 모드가 되지 않음)
builder.Configuration.AddSetupSettings();
// 환경 변수와 명령줄은 설치 결과 파일보다 우선하도록 다시 추가한다 (운영 배포에서 연결 문자열 등을 덮어쓸 수 있게)
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
var installed = SetupSettings.IsInstalled(builder.Configuration);

// 0-1. 외부 플러그인 어셈블리 탐색 (Plugins/*.dll)
var plugins = PluginManager.Init(builder.Environment.ContentRootPath);

// 1. MVC Razor 뷰 및 Web API 컨트롤러 일괄 구성 (BitPlus.Web 확장 + 플러그인)
// - 전역 환경(App.DevMode, App.Config) 초기화 및 IHttpContextAccessor 등록
// - System.Text.Json 일괄 포맷팅(CamelCase, DateFormatter 등)
// - ModelState 유효성 검사 실패 시 표준 Result.Fail 반환 및 에러 자동 로깅
// - BitPlus.Web 어셈블리 내장 컨트롤러(SystemController 등) 및 외부 플러그인 컨트롤러/뷰 자동 등록
var mvc = builder.AddControllersWithViews();
foreach (var plugin in plugins.Plugins) mvc.AddApplicationPart(plugin);

// 2. 핵심 인프라 서비스 등록 (로깅 및 개발용 CORS)
builder.Services.AddLogger(builder.Configuration);
builder.Services.AddCorsService();
builder.Services.AddResponseCompress(); // 응답 압축(Brotli/Gzip, CSS·JS·JSON·SVG 등 — HTML 제외)

// 3. 데이터베이스 및 엔티티 컨텍스트 등록 (WebConfig, AccountCacheManager, PolicyManager 포함)
// - DB 종류는 설정 "Database:Provider"(설치 시 기록)로 고른 프로바이더가 연결한다. 설치 전에는 연결하지 않는다.
var provider = DbProviders.Get(builder.Configuration[DbProviders.ProviderKey]);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SiteDbContext>(options =>
{
    if (installed && provider is not null && !string.IsNullOrWhiteSpace(connectionString))
        provider.UseProvider(options, connectionString, builder.Configuration[WebAppBuilderExtend.TablePrefixKey]);
    // 마이그레이션은 접두사 토큰으로 만들어져 실제 모델과 이름이 달라 보이므로 시작 시 마이그레이션(MigrateDatabaseAsync)이 모델 변경 경고로 멈추지 않게 한다
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// 4. ASP.NET Core Identity Core & 동적 옵션 등록 (AppUser, AppRole, Stores, Tokens)
builder.Services.AddIdentity();

// 5. 동적 권한 정책 인가(Policy Authorization) 등록
builder.Services.AddPolicyAuthorization();

// 6. 사이트 전용 다국어 리소스 매니저 등록 (+ 외부 플러그인 내장 리소스)
builder.Services.AddLangResource([BitPlus.Web.Site.Properties.Resources.ResourceManager, .. plugins.ResourceManagers]);

// 7. OpenAPI 서비스 등록
builder.Services.AddOpenApi();

// 7-1. 일간 방문 집계(페이지 요청 수·방문자 수, 업로드 폴더/Counter 파일)
builder.Services.AddVisitCounter();

// 7-2. 서비스 매니저: 시스템 이벤트(회원·컨텐츠·방문·오류)를 구독하는 서비스들(사이트 어셈블리 + 외부 플러그인 DLL) — 설정을 DB 에서 읽으므로 설치 후에만
if (installed)
{
    builder.AddServiceManager();
    var registered = builder.Services.Where(p => p.ServiceType == typeof(BitPlus.Web.Services.IService)).Select(p => p.ImplementationType).ToHashSet();
    foreach (var plugin in plugins.Plugins)
    {
        try
        {
            foreach (var type in plugin.GetTypes().Where(p => p.IsClass && !p.IsAbstract))
            {
                if (typeof(BitPlus.Web.Services.IService).IsAssignableFrom(type) && registered.Add(type))
                    builder.Services.AddSingleton(typeof(BitPlus.Web.Services.IService), type);
                if (typeof(BitPlus.Web.Search.ISearchProvider).IsAssignableFrom(type))
                    builder.Services.AddScoped(typeof(BitPlus.Web.Search.ISearchProvider), type);
            }
        }
        catch { }
    }
}

// 7-3. 위젯: 페이지 영역 읽기 서비스 + 위젯 데이터 로더 — 설치 후에만
if (installed) builder.Services.AddWidgets();

// 7-4. 외부 로그인(OAuth): 관리 화면 "계정 관리 > OAuth 구성" 의 서비스를 인증 스킴으로 (기본 스킴 없음, 콜백은 UseAuthentication) — 설치 후에만
if (installed) builder.Services.AddOAuthLogin();

var app = builder.Build();

// 설치된 사이트 업그레이드: 대기 중인 마이그레이션 적용 (DB 를 처음 쓰는 아래 단계들보다 먼저, 실패하면 로그만) — 설치 후에만
if (installed) await app.MigrateDatabaseAsync();

// [보안] 인증·암호화 마스터 키 로드 (캐시 → DB → appsettings, 없으면 생성해 DB 설정에 저장) — 설치 후에만
if (installed) await app.Services.GetRequiredService<WebConfig>().LoadSecretKeyAsync();

// 로그 제외 규칙: 설정 "LogExclude"(저장한 적 없으면 기본 규칙)를 적용하고 1분마다 다시 읽음(다른 서버의 변경 반영) — 설치 전에는 기본 규칙
if (installed) BitPlus.Logging.LogFilter.Watch(() => app.Services.GetRequiredService<WebConfig>().LogExcludeRules);

// 위젯·영역 폴더(Views/Widgets, Views/Sections)의 JSON 을 정보 테이블과 맞춤 (새 위젯 자동 등록) — 설치 후에만
if (installed) await app.SyncWidgetsAsync();

// 기본 다국어 문장(Data/Languages.json: 긴 안내 문장·메일 기본 본문)을 다국어 테이블에 맞춤 (없는 행만 추가) — 설치 후에만
if (installed) await app.SyncLanguagesAsync();

// 8. 오류 페이지(wwwroot/html: 상태 코드별 HTML, API 는 Result JSON) → 전역 에러 로깅 미들웨어 (미처리 예외 로깅 후 다시 던져 오류 페이지로 이어짐)
app.UseErrorPages();
app.UseErrorLogging();

// 9. 개발 환경용 CORS 활성화 (App.DevMode 시 AllowDevCors 적용)
app.UseCors();

// 10. 환경별 파이프라인 구성 (공통: OpenAPI JSON 규격 / 개발: Scalar API 문서 / 운영: HTTPS)
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}
else
{
    app.UseHTTPS();
}

// 11. 응답 압축(정적 파일보다 앞) → 정적 파일(웹 루트 + 외부 플러그인 wwwroot 임베디드 리소스 통합 서빙) 및 라우팅 파이프라인 구성
app.UseResponseCompress();
app.UsePluginStaticFiles(plugins);
app.UseRouting();

// 12. 동적 다국어 문화권 해석 미들웨어 (Query -> Cookie -> Header -> Browser -> Default)
app.UseCulture();

// 13. 설치 안내: 설치 전에는 모든 요청을 설치 마법사(/Setup)로 보내고, 설치 후에는 /Setup 을 막는다
app.UseSetupGuard(installed);

// 13-1. 일간 방문 집계: 페이지 요청마다 세고 그날 첫 방문이면 하루 쿠키 발급 — 업로드 폴더 설정(DB)을 쓰므로 설치 후에만
if (installed) app.UseVisitCounter();

// 14. 계정 인증 미들웨어 (쿠키 토큰 추출, RTR 회전, ClaimsPrincipal 주입 — 헤더 인증이 필요한 SPA 는 AddAccountTokenHandler(AuthMode.Header|Both) 등록)
// - DB 를 쓰므로 설치 후에만 등록한다
if (installed) app.UseAccount(AuthMode.Cookie);
app.UseAuthentication();
app.UseAuthorization();

// 15. 엔드포인트 매핑 (Web API 및 MVC View 라우트)
app.MapControllers();
app.MapDefaultControllerRoute();
app.Run();
