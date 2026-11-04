using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;

namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 페이지(<c>/content/{ruleNo}/{view?}</c>) 뷰 모델: 규칙 정보와 형식별 파샬뷰 경로.
/// </summary>
public class ContentPageModel
{
    /// <summary>
    /// <see cref="ContentPageModel"/> 기본 생성자.
    /// </summary>
    public ContentPageModel()
    {
        this.RuleNo = 0;
        this.Title = string.Empty;
        this.Type = ContentTypes.Default;
        this.View = ContentPageModel.IndexView;
        this.Permission = new ContentPermission();
        this.TopHtml = string.Empty;
        this.Categories = [];
        this.Feed = [];
        this.Options = [];
        this.BottomHtml = string.Empty;
        this.EditNo = 0;
        this.PostNo = 0;
        this.ReplyNo = 0;
        this.ReplyTitle = string.Empty;
        this.Post = null;
        this.NeedPassword = false;
        this.SideHidden = false;
        this.SideRight = false;
        this.SideMenuKey = null;
    }

    /// <summary>각 형식 폴더에서 처음 열리는 파샬뷰 이름</summary>
    public const string IndexView = "Index";

    /// <summary>컨텐츠 규칙 번호</summary>
    public int RuleNo { get; set; }

    /// <summary>현재 언어 규칙으로 고른 규칙 제목</summary>
    public string Title { get; set; }

    /// <summary>컨텐츠 형식 (형식 폴더 이름)</summary>
    public ContentTypes Type { get; set; }

    /// <summary>열 파샬뷰 이름 (확장자 제외, 첫 글자 대문자)</summary>
    public string View { get; set; }

    /// <summary>규칙 속성 키: 상단 영역 HTML (게시판 설정에서 입력, 저장할 때 정제됨)</summary>
    public const string TopHtmlKey = DefaultContentHandler.TopHtmlKey;

    /// <summary>규칙 속성 키: 하단 영역 HTML (게시판 설정에서 입력, 저장할 때 정제됨)</summary>
    public const string BottomHtmlKey = DefaultContentHandler.BottomHtmlKey;

    /// <summary>페이지 틀이 파샬뷰 위에 그대로 출력할 상단 영역 HTML (없으면 빈 문자열)</summary>
    public string TopHtml { get; set; }

    /// <summary>페이지 틀이 파샬뷰 아래에 그대로 출력할 하단 영역 HTML (없으면 빈 문자열)</summary>
    public string BottomHtml { get; set; }

    /// <summary>규칙 속성 키: 승인/기능 옵션 ({키: 불리언} 사전을 JSON 한 개로 묶은 값)</summary>
    public const string OptionsKey = ContentOptions.PropertyKey;

    /// <summary>글쓰기 화면에서 고를 카테고리 (값, 표시 글자 쌍 — 규칙 Category 컬럼을 풀어 놓은 것, 없으면 빈 목록)</summary>
    public List<KeyValuePair<string, string>> Categories { get; set; }

    /// <summary>규칙에 저장된 승인/기능 옵션 ({키: 불리언}). 저장된 적 없는 키는 <see cref="Option"/> 이 기본값으로 판단</summary>
    public Dictionary<string, bool> Options { get; set; }

    /// <summary>
    /// 승인/기능 옵션 값: 저장된 값이 있으면 그 값, 없으면 게시판 설정 화면의 기본 체크 상태입니다.
    /// </summary>
    /// <param name="key">옵션 키 (예: UseNotice, UseTags)</param>
    /// <returns>켜져 있으면 true</returns>
    public bool Option(string key) => ContentOptions.Value(this.Options, key);

    /// <summary>
    /// 규칙 Category 컬럼의 카테고리 문자열을 목록으로 풉니다 (<see cref="ContentOptions.ParseCategories"/> 와 같음).
    /// </summary>
    /// <param name="category">카테고리 문자열 (없으면 빈 목록)</param>
    /// <returns>(값, 표시 글자) 목록</returns>
    public static List<KeyValuePair<string, string>> ParseCategories(string? category) => ContentOptions.ParseCategories(category);

    /// <summary>
    /// 속성에 저장된 옵션 JSON({키: 불리언})을 사전으로 풉니다 (<see cref="ContentOptions.Parse"/> 와 같음).
    /// </summary>
    /// <param name="json">옵션 JSON</param>
    /// <returns>옵션 사전</returns>
    public static Dictionary<string, bool> ParseOptions(string? json) => ContentOptions.Parse(json);

    /// <summary>사이드 패널 감춤 여부 (게시판 설정의 사이드 구성)</summary>
    public bool SideHidden { get; set; }

    /// <summary>사이드 패널을 본문 우측에 둘지 여부 (아니면 좌측)</summary>
    public bool SideRight { get; set; }

    /// <summary>사이드 패널 연결 메뉴 식별키: 패널 제목에 이 메뉴 이름, 트리에 그 하위 메뉴 (null 이면 자동 = 컨텐츠맵 트리)</summary>
    public string? SideMenuKey { get; set; }

    /// <summary>
    /// 규칙 속성의 사이드 구성(감춤·위치·연결 메뉴)을 이 모델에 채웁니다.
    /// </summary>
    /// <param name="rule">컨텐츠 규칙 (속성 포함)</param>
    /// <returns>이 모델</returns>
    public ContentPageModel WithSide(ContentRule rule)
    {
        this.SideHidden = ContentManager.RuleProperty(rule, DefaultContentHandler.SideHiddenKey) == "true";
        this.SideRight = ContentManager.RuleProperty(rule, DefaultContentHandler.SidePositionKey) == DefaultContentHandler.SideRight;
        this.SideMenuKey = ContentManager.RuleProperty(rule, DefaultContentHandler.SideMenuKey);
        return this;
    }

    /// <summary>글쓰기 화면에서 수정할 글 번호 (<c>?no=</c>, 0 이면 새 글 작성)</summary>
    public int EditNo { get; set; }

    /// <summary>비밀번호 입력 화면(<c>PostPassword</c>)이 보내 줄 글 번호</summary>
    public int PostNo { get; set; }

    /// <summary>글쓰기 화면에서 답글을 쓸 원본 글 번호 (<c>?reply=</c>, 0 이면 답글이 아님)</summary>
    public int ReplyNo { get; set; }

    /// <summary>답글을 쓸 원본 글 제목 (제목 입력의 기본값 "RE: …" 에 씀)</summary>
    public string ReplyTitle { get; set; }

    /// <summary>글쓰기 화면에서 비밀번호 입력이 필요한지 여부 (비회원이 쓰는 경우: 새 글은 등록할 비밀번호, 수정은 본인 확인)</summary>
    public bool NeedPassword { get; set; }

    /// <summary>글 보기 화면(View)에 보일 글 (다른 화면에서는 null)</summary>
    public ContentPostItem? Post { get; set; }

    /// <summary>글 보기 화면이 블로그 피드 안에 그려지는지 (게시판 제목줄·목록/글쓰기 버튼을 빼고, 비회원 글 비밀번호·댓글 이동 주소는 쓰지 않음)</summary>
    public bool InFeed { get; set; }

    /// <summary>블로그 피드(<c>Blog/Index</c>)의 이 쪽 글: 글번호, 제목, 비회원 글(비밀번호 확인 필요) 여부, 글 보기 모델(열 수 없거나 비밀번호 확인이 필요한 글은 null)</summary>
    public List<(int No, string Title, bool Gate, ContentPageModel? Page)> Feed { get; set; }

    /// <summary>블로그 피드가 글 주소(<c>/content/{규칙}/view/{번호}</c> — 통합 검색·알림 등)로 들어와 그 글 하나만 보이는지 (피드 모델과 그 글의 글 보기 모델 모두에 표시)</summary>
    public bool SinglePost { get; set; }

    /// <summary>블로그 피드 안 글 보기의 댓글 수 (접힌 댓글 영역 제목줄에 보임)</summary>
    public int CommentCount { get; set; }

    /// <summary>블로그 피드의 지금 쪽 (1부터)</summary>
    public int Page { get; set; }

    /// <summary>블로그 피드의 전체 쪽 수</summary>
    public int PageCount { get; set; }

    /// <summary>블로그 피드의 전체 글 수</summary>
    public int Total { get; set; }

    /// <summary>블로그 피드가 승인 목록 모드(미승인 글만)인지</summary>
    public bool Pending { get; set; }

    /// <summary>미승인 글 수 (관리자·승인자만, 아니면 0)</summary>
    public int PendingCount { get; set; }

    /// <summary>현재 사용자의 이 규칙에 대한 권한</summary>
    public ContentPermission Permission { get; set; }

    /// <summary>목록 화면에서 선택할 수 있는 페이지당 글 수 (첫 값이 기본, 목록은 API 로 불러옴)</summary>
    public static int[] PageSizes => DefaultContentHandler.PageSizes;

    /// <summary>
    /// 형식 폴더 안의 파샬뷰 경로를 만듭니다. (예: ~/Views/Content/Forum/List.cshtml)
    /// </summary>
    /// <param name="view">파샬뷰 이름</param>
    /// <returns>뷰 경로</returns>
    public string PathOf(string view) => $"~/Views/Content/{this.Type}/{view}.cshtml";

    /// <summary>현재 파샬뷰 경로</summary>
    public string ViewPath => this.PathOf(this.View);

    /// <summary>
    /// 신규 게시판으로 고를 수 있는 형식: 설정(Manage) 화면이 있는 형식만 (없는 형식은 설정 화면이 404 라서 뺌).
    /// </summary>
    /// <param name="viewEngine">뷰 엔진 (없으면 빈 목록)</param>
    /// <returns>형식 목록 (<see cref="BitPlus.Web.Site.Data.BoardAccess.BoardTypes"/> 순서, 그 밖의 형식은 뒤에 정의 순서)</returns>
    public static List<ContentTypes> ManageTypes(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine? viewEngine)
        => [.. Enum.GetValues<ContentTypes>().OrderBy(t => Array.IndexOf(BitPlus.Web.Site.Data.BoardAccess.BoardTypes, t) is var i and >= 0 ? i : int.MaxValue).Where(t => viewEngine?.GetView(null, new ContentPageModel { Type = t, View = "Manage" }.ViewPath, false).Success == true)];
}
