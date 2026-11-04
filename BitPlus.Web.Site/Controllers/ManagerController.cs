using BitPlus.Web.HTTP.View;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 관리자 콘솔 대시보드 및 시스템 관리 뷰를 렌더링하는 MVC 컨트롤러입니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public class ManagerController : ViewControllerBase
{
    /// <summary>
    /// <see cref="ManagerController"/> 기본 생성자.
    /// </summary>
    public ManagerController() : base() { }

    /// <summary>
    /// 관리자 대시보드 메인 인덱스 뷰를 반환합니다.
    /// </summary>
    /// <returns>관리자 대시보드 뷰 결과</returns>
    [HttpGet]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Index()
    {
        this.SetPageKey("Dashboard");
        return this.View();
    }

    /// <summary>
    /// 계정 관리 컨테이너 뷰(상단 탭 + iframe 뷰어)를 반환합니다.
    /// 실제 탭 콘텐츠는 <see cref="AccountUsers"/>, <see cref="AccountRole"/>,
    /// <see cref="AccountTerms"/>, <see cref="AccountTemplate"/>, <see cref="AccountConfig"/> 가
    /// 각자 독립된 iframe 대상 페이지로 제공합니다.
    /// </summary>
    /// <returns>계정 관리 컨테이너 뷰</returns>
    [HttpGet]
    public IActionResult Account()
    {
        this.SetPageKey("Account");
        return this.View();
    }

    /// <summary>
    /// 계정 관리 - 사용자 및 조직 관리 iframe 대상 뷰 (Layout 없음, 독립 렌더링).
    /// </summary>
    /// <returns>사용자 및 조직 관리 뷰</returns>
    [HttpGet("Manager/Account/Users")]
    public IActionResult AccountUsers() => this.View("Account/Users");

    /// <summary>
    /// 계정 관리 - 역할(Role) 관리 iframe 대상 뷰 (Layout 없음, 독립 렌더링).
    /// </summary>
    /// <returns>역할 관리 뷰</returns>
    [HttpGet("Manager/Account/Role")]
    public IActionResult AccountRole() => this.View("Account/Role");

    /// <summary>
    /// 계정 관리 - 권한 정책(구성 파일 + DB 병합) 관리 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>정책 관리 뷰</returns>
    [HttpGet("Manager/Account/Policy")]
    public IActionResult AccountPolicy() => this.View("Account/Policy");

    /// <summary>
    /// 계정 관리 - 약관(Terms) 관리 iframe 대상 뷰 (Layout 없음, 독립 렌더링).
    /// </summary>
    /// <returns>약관 관리 뷰</returns>
    [HttpGet("Manager/Account/Terms")]
    public IActionResult AccountTerms() => this.View("Account/Terms");

    /// <summary>
    /// 계정 관리 - 메일 템플릿 관리 iframe 대상 뷰 (Layout 없음, 독립 렌더링).
    /// </summary>
    /// <returns>메일 템플릿 관리 뷰</returns>
    [HttpGet("Manager/Account/Template")]
    public IActionResult AccountTemplate() => this.View("Account/Template");

    /// <summary>
    /// 계정 관리 - 인증/토큰 정책 관리 iframe 대상 뷰 (Layout 없음, 독립 렌더링).
    /// </summary>
    /// <returns>인증/토큰 정책 관리 뷰</returns>
    [HttpGet("Manager/Account/Config")]
    public IActionResult AccountConfig() => this.View("Account/Config");

    /// <summary>
    /// 계정 관리 - 외부 로그인(OAuth) 서비스 구성 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>OAuth 구성 뷰</returns>
    [HttpGet("Manager/Account/OAuth")]
    public IActionResult AccountOAuth() => this.View("Account/OAuth");

    /// <summary>
    /// 컨텐츠 관리 페이지 — 탭(게시판, 페이지 컨텐츠)으로 컨텐츠를 관리하는 뷰를 반환합니다. 게시판 설정 화면은 <c>/Manager/Content/Board/{ruleNo}</c>(<see cref="ContentController.ManagerBoard"/>)입니다.
    /// </summary>
    /// <returns>컨텐츠 관리 뷰</returns>
    [HttpGet]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Content()
    {
        this.SetPageKey("Content");
        return this.View();
    }

    /// <summary>
    /// 컨텐츠 관리 - 게시판(컨텐츠 규칙) 목록 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>게시판 목록 조각 뷰</returns>
    [HttpGet("Manager/Content/Boards")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ContentBoards() => this.View("Content/Boards");

    /// <summary>
    /// 컨텐츠 관리 - 파일 규칙 관리 탭 조각 뷰 (Layout 없음, 신고 탭 앞).
    /// </summary>
    /// <returns>파일 규칙 관리 조각 뷰</returns>
    [HttpGet("Manager/Content/FileRule")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ContentFileRule() => this.View("Content/FileRule");

    /// <summary>
    /// 컨텐츠 관리 - 게시물·댓글 신고 목록 탭 조각 뷰 (Layout 없음, 상세·처리 창 포함).
    /// </summary>
    /// <returns>신고 목록 조각 뷰</returns>
    [HttpGet("Manager/Content/Reports")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ContentReports() => this.View("Content/Reports");

    /// <summary>
    /// 컨텐츠 관리 - 페이지 컨텐츠(보기 전용 규칙의 <c>/view/{글 번호}</c> 페이지) 목록 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>페이지 컨텐츠 목록 조각 뷰</returns>
    [HttpGet("Manager/Content/Pages")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ContentPages() => this.View("Content/Pages");

    /// <summary>
    /// 시스템(전역) 설정 페이지 — 탭으로 설정 묶음을 나눠 보여주는 뷰를 반환합니다.
    /// </summary>
    /// <returns>시스템 설정 뷰</returns>
    [HttpGet]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult System()
    {
        this.SetPageKey("System");
        return this.View();
    }

    /// <summary>
    /// 서비스 관리 - 사이트 메뉴 관리 탭 조각 뷰 (Layout 없음, 팝업 탭 앞).
    /// </summary>
    /// <returns>사이트 메뉴 관리 조각 뷰</returns>
    [HttpGet("Manager/Service/Menu")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ServiceMenu() => this.View("Service/Menu");

    /// <summary>
    /// 시스템 설정 - 다국어 관리 탭 조각 뷰 (Layout 없음): Languages 테이블을 키(범주 + 키) 기준으로 관리.
    /// </summary>
    /// <returns>다국어 관리 조각 뷰</returns>
    [HttpGet("Manager/System/Language")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult SystemLanguage() => this.View("System/Language");

    /// <summary>
    /// 시스템 설정 - 설정 관리 탭 조각 뷰 (Layout 없음): Configs 테이블 키-값을 직접 고치거나 등록.
    /// </summary>
    /// <returns>설정 관리 조각 뷰</returns>
    [HttpGet("Manager/System/Config")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult SystemConfig() => this.View("System/Config");

    /// <summary>
    /// 시스템 설정 - 로그 탭 조각 뷰 (Layout 없음): 파일 로그를 종류·날짜별로 고르고 시간·수준·검색어로 걸러 보기.
    /// </summary>
    /// <returns>로그 조각 뷰</returns>
    [HttpGet("Manager/System/Log")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult SystemLog() => this.View("System/Log");

    /// <summary>
    /// 위젯 설정 양식 조각 (Layout 없음): <c>Views/Widgets/{키}/Edit.cshtml</c> — 페이지 위 편집 모드의 위젯 모달에 붙입니다 (입력 name = 설정 키).
    /// </summary>
    /// <param name="key">위젯 키 (영문·숫자, 폴더 이름)</param>
    /// <returns>설정 양식 조각 뷰 (없으면 404)</returns>
    [HttpGet("Manager/Widget/Edit/{key}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult WidgetEdit(string key)
    {
        if (!Widgets.WidgetCatalog.KeyPattern().IsMatch(key)) return this.NotFound();
        var view = $"~/Views/Widgets/{key}/Edit.cshtml";
        var engine = this.HttpContext.GetService<ICompositeViewEngine>()!;
        return engine.GetView(null, view, false).Success ? this.View(view) : this.NotFound();
    }

    /// <summary>
    /// 시스템 설정 - 사이트 정보(문서 머리·상하단 표시·회사 정보) 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>사이트 정보 조각 뷰</returns>
    [HttpGet("Manager/System/Site")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult SystemSite() => this.View("System/Site");

    /// <summary>
    /// 외부 서비스 설정 페이지 — 탭(SMTP, 외부 연동, Redis, AI API, 접근 토큰)으로 외부 서비스 접속 정보를 나눠 보여주는 뷰를 반환합니다.
    /// </summary>
    /// <returns>외부 서비스 뷰</returns>
    [HttpGet]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult External()
    {
        this.SetPageKey("External");
        return this.View();
    }

    /// <summary>
    /// 외부 서비스 - 메일(SMTP) 구성 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>SMTP 구성 조각 뷰</returns>
    [HttpGet("Manager/External/Smtp")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ExternalSmtp() => this.View("External/Smtp");

    /// <summary>
    /// 외부 서비스 - 외부 연동(카카오 등 외부 서비스 공개 키) 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>외부 연동 조각 뷰</returns>
    [HttpGet("Manager/External/Integration")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ExternalIntegration() => this.View("External/Integration");

    /// <summary>
    /// 외부 서비스 - Redis 캐시 구성 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>Redis 구성 조각 뷰</returns>
    [HttpGet("Manager/External/Redis")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ExternalRedis() => this.View("External/Redis");

    /// <summary>
    /// 외부 서비스 - AI API 접속 정보 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>AI API 조각 뷰</returns>
    [HttpGet("Manager/External/Ai")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ExternalAi() => this.View("External/Ai");

    /// <summary>
    /// 외부 서비스 - 외부 API 접근 토큰 발급·관리 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>접근 토큰 조각 뷰</returns>
    [HttpGet("Manager/External/Token")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ExternalToken() => this.View("External/Token");

    /// <summary>
    /// 서비스 관리 페이지 — 탭(이벤트 서비스, 사이트 메뉴, 팝업) 컨테이너 뷰를 반환합니다. 탭 내용은 조각 뷰(<see cref="ServiceList"/>, <see cref="ServiceMenu"/>, <see cref="ServicePopup"/>)입니다.
    /// </summary>
    /// <returns>서비스 관리 뷰</returns>
    [HttpGet]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Service()
    {
        this.SetPageKey("Service");
        return this.View();
    }

    /// <summary>
    /// 서비스 관리 - 서비스 탭 조각 뷰 (Layout 없음): 등록된 서비스(이벤트 구독 서비스) 목록·상태·활성 전환, 항목별 설정 모달.
    /// </summary>
    /// <returns>서비스 목록 조각 뷰</returns>
    [HttpGet("Manager/Service/List")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ServiceList() => this.View("Service/List");

    /// <summary>
    /// 서비스 관리 - 팝업 관리 탭 조각 뷰 (Layout 없음).
    /// </summary>
    /// <returns>팝업 관리 조각 뷰</returns>
    [HttpGet("Manager/Service/Popup")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ServicePopup() => this.View("Service/Popup");

    /// <summary>
    /// 서비스 관리 - 서비스 설정 양식 조각 뷰 (Layout 없음, 설정 모달에 붙음). 서비스 이름과 같은 <c>Views/Manager/Service/Forms/{이름}.cshtml</c>(사이트 또는 플러그인 DLL)을 찾고, 없으면 빈 조각입니다.
    /// </summary>
    /// <param name="name">서비스 이름</param>
    /// <returns>설정 양식 조각 뷰 (모르는 서비스면 404)</returns>
    [HttpGet("Manager/Service/Form/{name}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult ServiceForm(string name)
    {
        if (this.HttpContext.GetService<ServiceManager>()?.Find(name) is not { } service) return this.NotFound();
        var view = $"Service/Forms/{service.Name}"; // 등록된 서비스 이름만 뷰 경로로 씀
        var engine = this.HttpContext.GetService<ICompositeViewEngine>()!;
        return engine.FindView(this.ControllerContext, view, false).Success ? this.View(view, service) : this.Content(string.Empty, "text/html");
    }

    /// <summary>
    /// UI 개발지원 페이지 — HTML 샘플 iframe 갤러리 뷰를 반환합니다.
    /// </summary>
    /// <returns>DevKit 뷰</returns>
    [HttpGet]
    public IActionResult DevKit()
    {
        this.SetPageKey("DevKit");
        return this.View();
    }}
