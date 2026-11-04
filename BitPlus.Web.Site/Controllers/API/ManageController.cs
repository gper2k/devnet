using BitPlus.Data.Account;
using BitPlus.Web.Data.Content;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Site.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// AI 에이전트 및 외부 자동화 시스템을 위한 사이트 통합 관리 Web API 컨트롤러 (api/manage).
/// 비즈니스 로직을 별도로 중복 구현하지 않고, 기존 컨트롤러(Content, Membership, SiteMap, Log, Config) 및 공통 매니저로 위임(Thin Facade)하여 일관된 단일 창구를 제공합니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
[Route("api/[controller]")]
public partial class ManageController : APIControllerBase
{
    /// <summary>
    /// <see cref="ManageController"/> 기본 생성자.
    /// </summary>
    public ManageController() : base() { }

    /// <summary>컨텐츠 매니저</summary>
    private ContentManager Manager => new(this);

    /// <summary>사이트맵 메뉴 컨트롤러 위임 객체</summary>
    private SiteMapController SiteMap => new() { ControllerContext = this.ControllerContext };

    /// <summary>회원 및 권한 컨트롤러 위임 객체</summary>
    private MembershipController Membership => new() { ControllerContext = this.ControllerContext };

    /// <summary>로그 컨트롤러 위임 객체</summary>
    private LogController LogCtrl => new() { ControllerContext = this.ControllerContext };

    /// <summary>설정 컨트롤러 위임 객체</summary>
    private ConfigController ConfigCtrl => new() { ControllerContext = this.ControllerContext };

    /// <summary>컨텐츠 컨트롤러 위임 객체</summary>
    private ContentController ContentCtrl => new() { ControllerContext = this.ControllerContext };

    /// <summary>방문자 집계 서비스</summary>
    private VisitCounter? Counter => this.HttpContext.GetService<VisitCounter>();

    /// <summary>
    /// 컨텐츠 응답 객체를 표준 API 결과로 변환합니다.
    /// </summary>
    /// <param name="response">컨텐츠 응답</param>
    /// <returns>API 결과</returns>
    private IActionResult Respond(ContentResponse response) => response.Status switch
    {
        200 => this.Complete(response.Data, response.Message),
        401 => this.UnauthorizedResult(response.Message),
        403 => this.ForbiddenResult(response.Message),
        404 => this.NotFoundResult(response.Message),
        _ => this.Error(response.Message ?? WebResources.ValidRegex, response.Data, response.Status)
    };

    /// <summary>
    /// AI 에이전트 연동 상태 및 서버 생존을 확인합니다 (Ping).
    /// </summary>
    /// <returns>서버 상태 및 현재 시각 정보</returns>
    [HttpGet("ping")]
    public IActionResult Ping() => this.Complete(new
    {
        status = "OK",
        serverUtc = DateTime.UtcNow,
        serverLocal = DateTime.Now,
        version = Environment.Version.ToString(), // .NET 런타임 버전
        appVersion = App.Version, // 사이트 버전 (git 태그, MinVer)
        os = Environment.OSVersion.VersionString,
        caller = this.CurrentUserName
    });

    /// <summary>
    /// 사이트 운영 전반의 종합 요약 현황(회원 수, 게시글 수, 메뉴 수, 오늘 방문자 통계 등)을 조회합니다.
    /// </summary>
    /// <returns>운영 종합 지표</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalUsers = await this.IdentityDb.Users.AsNoTracking().CountAsync();
        var activeUsers = await this.IdentityDb.Users.AsNoTracking().CountAsync(p => p.Enabler);
        var totalBoards = await this.DbContext!.ContentRules.AsNoTracking().CountAsync();
        var totalPosts = await this.DbContext.Contents.AsNoTracking().CountAsync();
        var totalMenus = await this.DbContext.Menus.AsNoTracking().CountAsync();

        var todayVisits = this.Counter?.Day(VisitCounter.Today);

        return this.Complete(new
        {
            users = new { total = totalUsers, active = activeUsers },
            content = new { boards = totalBoards, posts = totalPosts },
            menus = new { total = totalMenus },
            visits = new
            {
                todayPages = todayVisits?.Pages ?? 0,
                todayVisitors = todayVisits?.Visitors ?? 0
            },
            time = DateTime.UtcNow
        });
    }
}
