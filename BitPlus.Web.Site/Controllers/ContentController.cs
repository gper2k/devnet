using BitPlus.Data.Content;
using BitPlus.Data.CommonModel;
using BitPlus.Data.SiteMap;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Data.Model;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.HTTP.View;
using BitPlus.Web.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 컨텐츠 공개 페이지를 렌더링하는 MVC 뷰 컨트롤러입니다: 규칙 번호가 없는 <c>/content</c> 는 컨텐츠 포탈, 규칙 번호가 있는 <c>/content/{ruleNo}</c> 는 그 규칙의 형식별 페이지입니다. 뷰는 <c>Views/Content</c> 폴더(형식별 하위 폴더)에 있습니다.
/// (REST API 는 <c>Controllers/API/ContentController</c>)
/// </summary>
public partial class ContentController : ViewControllerBase
{
    /// <summary>
    /// <see cref="ContentController"/> 기본 생성자.
    /// </summary>
    public ContentController() : base() { }

    /// <summary>
    /// 컨텐츠 포탈: 규칙 번호 없이 <c>/content</c> 로 들어오면 왼쪽 사이드 패널(컨텐츠맵 트리 위젯 …)과 위젯 페이지 <c>portal</c> 을 보여 줍니다.
    /// </summary>
    /// <returns>컨텐츠 포탈 뷰 결과 (모델: <see cref="ContentPortalModel"/>)</returns>
    [HttpGet("/content")]
    public IActionResult Portal()
    {
        this.SetTitle(this.Request.GetLanguage("ContentManage") ?? "Content");
        this.SetPageKey("Content");
        return this.View(new ContentPortalModel());
    }

    /// <summary>
    /// 메뉴별 컨텐츠 포탈: <c>/content/menu/{menuKey}</c> — 기본 포탈과 같은 화면이되 위젯 배치를 그 메뉴 키로 따로 둡니다(위젯 페이지 <c>portal@{메뉴 키}</c>, 따로 저장한 적 없으면 기본 포탈 배치).
    /// 사이드 패널은 그 메뉴를 연결 메뉴로 씁니다 (트리 루트 = 그 메뉴, 사이드 위젯 배치 <c>side@{메뉴 키}</c>). 페이지 제목은 메뉴 표시 이름입니다.
    /// 메뉴 키 형식이 아니거나 메뉴가 없거나 그 메뉴·상위 메뉴 중 사용 중이 아닌 것이 있으면 404, 그 메뉴·상위 메뉴 중 접근 권한이 없는 것이 있으면 비로그인 401·로그인 403 입니다
    /// (사이드 트리처럼 상위 메뉴가 빠지면 그 아래도 빠짐).
    /// </summary>
    /// <param name="menuKey">메뉴 식별키</param>
    /// <returns>컨텐츠 포탈 뷰 결과 (모델: <see cref="ContentPortalModel"/>, <see cref="ContentPortalModel.MenuKey"/> 에 메뉴 키)</returns>
    [HttpGet("/content/menu/{menuKey}")]
    public async Task<IActionResult> MenuPortal(string menuKey)
    {
        if (!MenuKeyRegex().IsMatch(menuKey)) return this.NotFound();
        var db = this.DbContext!;
        var menu = await db.Menus.AsNoTracking().AsSplitQuery().Include(p => p.DpNames).Include(p => p.Accesses).FirstOrDefaultAsync(p => p.Key == menuKey && p.Enabler);
        if (menu is null) return this.NotFound();
        var ancestorKeys = (menu.Location ?? string.Empty).Split(AppMenu.LocationSeparator, StringSplitOptions.RemoveEmptyEntries);
        var ancestors = ancestorKeys.Length == 0 ? [] : await db.Menus.AsNoTracking().Include(p => p.Accesses).Where(p => ancestorKeys.Contains(p.Key)).ToListAsync();
        if (ancestors.Count != ancestorKeys.Length || ancestors.Any(p => !p.Enabler)) return this.NotFound();
        if (!ancestors.Append(menu).All(p => MenuReader.IsAllowed(p, this.CurrentUserAssets))) return this.Denied();

        this.SetTitle(this.Request.DisplayName(menu.DpNames, menu.Key));
        this.SetPageKey("Content");
        return this.View(nameof(Portal), new ContentPortalModel { MenuKey = menu.Key });
    }

    /// <summary>메뉴 키 형식 (메뉴 편집 API·위젯 배치 변형 키와 같음): 영문·숫자·밑줄·하이픈·점 64자 이내</summary>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial System.Text.RegularExpressions.Regex MenuKeyRegex();

    /// <summary>
    /// 컨텐츠 페이지: <c>/content/{ruleNo}/{view?}</c> 의 규칙 번호로 컨텐츠 규칙을 찾아, 그 규칙 형식 폴더(Views/Content/{형식})의 파샬뷰를 페이지 안에 보여 줍니다.
    /// view 가 없으면 <c>Index</c>, 있으면 그 이름의 뷰(예: /content/1/list?page=0 → Views/Content/{형식}/List.cshtml)입니다.
    /// 규칙이 없거나 비활성이거나 보기 전용(OnlyView, /view/{contentNo} 용)이거나, view 이름이 형식(영문·숫자·밑줄)에 맞지 않거나 그 형식 폴더에 뷰가 없으면 404 입니다.
    /// 규칙 권한(<see cref="ContentPermission"/>): 읽을 수 없으면, 쓰기 화면(<c>write</c>)은 쓰기 권한(읽기 권한과 별개)이 없으면 비로그인은 401, 로그인 사용자는 403 입니다. (<c>manage</c> 는 <c>/content/manage/{ruleNo}</c> 로 보냄)
    /// 목록 화면(<c>Index</c>·<c>List</c>)이면 공개된 글(표시 + 게시, 승인된 글, 관리자는 모두)을 공지 먼저 최신순으로 한 페이지(<c>?page=</c> 1부터, <c>?size=</c> 페이지당 글 수) 불러옵니다.
    /// (라우트 값 이름 "action" 은 MVC 예약어라 view 로 받음)
    /// ponytail: 글 읽기 권한(ReadRule)·승인 옵션·검색·카테고리는 아직 처리하지 않음 — 컨텐츠 본문 기능과 함께 추가
    /// </summary>
    /// <param name="ruleNo">컨텐츠 규칙 번호</param>
    /// <param name="view">파샬뷰 이름 (생략 시 Index)</param>
    /// <returns>컨텐츠 페이지 뷰 결과 (모델: <see cref="ContentPageModel"/>)</returns>
    [HttpGet("/content/{ruleNo:int}/{view?}")]
    public async Task<IActionResult> Index(int ruleNo, string? view)
    {
        if (view is not null && !ViewNameRegex().IsMatch(view)) return this.NotFound();
        if (string.Equals(view, ManageView, StringComparison.OrdinalIgnoreCase)) return this.Redirect($"/content/manage/{ruleNo}"); // 설정은 /content/manage/{규칙 번호}
        if (string.Equals(view, PostView, StringComparison.OrdinalIgnoreCase) || string.Equals(view, PostPasswordView, StringComparison.OrdinalIgnoreCase)) return this.NotFound(); // 글 보기는 /content/{규칙}/view/{글 번호} 또는 /content/view/{글 키}

        var db = this.DbContext!;
        var rule = await db.ContentRules.AsNoTracking().Include(p => p.Title).Include(p => p.AccessRule).Include(p => p.Approvers).Include(p => p.Properties).FirstOrDefaultAsync(p => p.RuleNo == ruleNo && p.Enabler && !p.OnlyView);
        if (rule is null) return this.NotFound();

        var model = this.PageModelOf(rule, string.IsNullOrEmpty(view) ? ContentPageModel.IndexView : char.ToUpperInvariant(view[0]) + view[1..]);
        if (!this.HttpContext.GetService<ICompositeViewEngine>()!.GetView(null, model.ViewPath, false).Success) return this.NotFound();
        // 목록·글 보기 등은 읽기 권한이 있어야 한다(없으면 401/403 오류 페이지). 쓰기 화면은 쓰기 권한만 필요(읽기 없이 쓰기만 줄 수 있음)
        // 화면별 필요 권한: 글쓰기는 쓰기(답글 쓰기 ?reply= 는 답글) 권한, 목록(Index·List)은 목록 권한, 그 밖의 화면은 읽기 권한
        var replying = model.View == WriteView && int.TryParse(this.Request.Query["reply"], out var replyQuery) && replyQuery > 0;
        var editing = model.View == WriteView && int.TryParse(this.Request.Query["no"], out var editQuery) && editQuery > 0; // 이미 있는 글(답글 포함) 수정은 쓰기 또는 답글 권한 (어느 쪽인지는 글을 불러올 때 서버가 최종 확인)
        var allowed = model.View == WriteView ? (editing ? model.Permission.CanWrite || model.Permission.CanAnswer : replying ? model.Permission.CanAnswer : model.Permission.CanWrite) : model.View is ContentPageModel.IndexView or ListView ? model.Permission.CanList : model.Permission.CanRead;
        if (!allowed) return this.Denied();
        if (rule.Type == ContentTypes.Blog && model.View is ContentPageModel.IndexView or ListView) await this.FillFeedAsync(rule, model); // 블로그: 목록 없이 글 보기를 이어서 보이는 피드
        if (model.View == WriteView)
        {
            model.EditNo = int.TryParse(this.Request.Query["no"], out var no) && no > 0 ? no : 0; // 글 번호가 있으면 수정 (기존 글은 화면에서 API 로 불러와 채움: 본인 글 확인 포함)
            model.NeedPassword = !this.IsAuthenticated; // 비회원: 새 글은 등록할 비밀번호, 수정은 본인 확인용 비밀번호
            if (model.EditNo > 0 && !model.Permission.IsAdmin && await db.Contents.AnyAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == model.EditNo && p.WriterId == null)) model.NeedPassword = true; // 비회원이 쓴 글은 로그인한 사용자도 비밀번호로 본인 확인 후 수정
            if (model.EditNo == 0 && int.TryParse(this.Request.Query["reply"], out var replyNo) && replyNo > 0) // ?reply= : 답글 쓰기 (원본 글을 열 수 있어야 하고 "답글 허용" 옵션이 켜져 있어야 함)
            {
                var parent = await db.Contents.AsNoTracking().Include(p => p.ReadRule).Include(p => p.Properties).FirstOrDefaultAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == replyNo);
                if (parent is null || !parent.Published || parent.IsNotice || ForumContentHandler.UsesTopicComments(rule.Type) || (rule.Type == ContentTypes.Manual && (parent.LevelNo ?? 0) > 0) || !PostAccessRule.IsApproved(rule, parent) || !model.Option("AllowAnswer") || PostAccessRule.Check(rule, model.Permission, parent, this.CurrentUserAssets, false, DateTime.UtcNow) != PostAccessRule.Allowed) return this.NotFound();
                (model.ReplyNo, model.ReplyTitle) = (replyNo, parent.Title);
            }
        }

        this.SetTitle(model.Title);
        this.SetPageKey("Content");
        return this.View(model);
    }

    /// <summary>
    /// 글 보기: <c>/content/{ruleNo}/view/{contentNo}</c> 는 그 규칙의 글 번호 글, <c>/content/view/{contentKey}</c> 는 글 식별키(Guid)로 글과 그 글의 규칙을 찾아 같은 글 보기 화면을 보여 줍니다.
    /// 화면은 그 규칙 형식 폴더의 <c>View</c> 뷰(페이지 틀 <c>Views/Content/Index.cshtml</c> 안)이고 없는 형식이면 404 입니다. 규칙이 없거나 비활성이거나 보기 전용(OnlyView)이면 404, 규칙을 읽을 수 없으면 비로그인 401·로그인 403 입니다.
    /// 글 공개 조건: 표시 + 게시 + 승인된 글이고 게시 기간 안이어야 하며(아니면 404), 관리자와 글쓴이 본인은 모두 볼 수 있습니다. 규칙의 "비공개 옵션"(<c>UsePrivate</c>)이 켜져 있고 글쓴이가 그 글을 비공개로 올렸으면 관리자와 글쓴이 본인만 볼 수 있고,
    /// 글에 읽기 권한 대상(게시 대상)이 있으면 그 대상에 속한 로그인 사용자만 볼 수 있습니다 (아니면 403). 익명 옵션(<c>UseAnonymous</c>) 규칙은 관리자가 아니면 글쓴이 이름 대신 "익명"을 보여 줍니다.
    /// 조회수는 중복을 배제해 올립니다(회원은 조회 기록이 없을 때만, 비회원은 글 속성 reader 의 IP 목록에 없을 때만 — IP 는 최대 100개, 넘으면 앞에서부터 삭제). 본문은 에디터가 정제해 저장한 HTML 이며 화면에서 샌드박스 프레임으로 한 번 더 격리해 출력합니다.
    /// </summary>
    /// <param name="ruleNo">컨텐츠 규칙 번호</param>
    /// <param name="contentNo">규칙 안의 글 번호</param>
    /// <returns>글 보기 뷰 결과 (모델: <see cref="ContentPageModel"/>, <see cref="ContentPageModel.Post"/> 에 글)</returns>
    [HttpGet("/content/{ruleNo:int}/view/{contentNo:int}")]
    public Task<IActionResult> Post(int ruleNo, int contentNo) => this.PostPageAsync(ruleNo, contentNo, null);

    /// <summary>
    /// 비회원 글의 본인 확인: <c>/content/{ruleNo}/view/{contentNo}</c> 로 비밀번호를 보내 일치하면 본인 글로 보고 글을 보여 줍니다 (비밀번호는 주소가 아니라 본문으로 받음).
    /// </summary>
    /// <param name="ruleNo">컨텐츠 규칙 번호</param>
    /// <param name="contentNo">규칙 안의 글 번호</param>
    /// <param name="password">글을 쓸 때 입력한 비밀번호</param>
    /// <returns>글 보기 뷰 결과 (틀리면 비밀번호 입력 화면)</returns>
    [HttpPost("/content/{ruleNo:int}/view/{contentNo:int}")]
    public Task<IActionResult> PostWithPassword(int ruleNo, int contentNo, [FromForm] string? password) => this.PostPageAsync(ruleNo, contentNo, null, password ?? string.Empty);

    /// <summary>
    /// 글 키로 글 보기: <c>/content/view/{contentKey}</c> — 글 식별키로 글과 규칙을 찾아 <see cref="Post"/> 와 같은 글 보기 화면을 보여 줍니다.
    /// </summary>
    /// <param name="contentKey">글 식별키</param>
    /// <returns>글 보기 뷰 결과</returns>
    [HttpGet("/content/view/{contentKey:guid}")]
    public Task<IActionResult> PostByKey(Guid contentKey) => this.PostPageAsync(0, 0, contentKey);

    /// <summary>
    /// 글 보기 페이지를 만듭니다 (<see cref="Post"/>·<see cref="PostByKey"/> 공통): 글과 규칙 찾기, 규칙 읽기 권한, 글 공개 조건, 조회수·조회 기록, 모델 순서로 처리합니다.
    /// </summary>
    private async Task<IActionResult> PostPageAsync(int ruleNo, int contentNo, Guid? contentKey, string? password = null)
    {
        var db = this.DbContext!;
        ContentRule? rule;
        ContentModel? content;

        if (contentKey != null)
        {
            content = await this.PostQuery().FirstOrDefaultAsync(p => p.Id == contentKey);
            if (content is null) return this.NotFound();

            rule = await db.ContentRules.AsNoTracking().AsSplitQuery()
                .Include(p => p.Title)
                .Include(p => p.AccessRule)
                .Include(p => p.Approvers)
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(p => p.Id == content.ContentRuleId && p.Enabler && !p.OnlyView);
            if (rule is null) return this.NotFound();
        }
        else
        {
            rule = await db.ContentRules.AsNoTracking().AsSplitQuery()
                .Include(p => p.Title)
                .Include(p => p.AccessRule)
                .Include(p => p.Approvers)
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(p => p.RuleNo == ruleNo && p.Enabler && !p.OnlyView);
            if (rule is null) return this.NotFound();

            content = await this.PostQuery().FirstOrDefaultAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == contentNo);
            if (content is null) return this.NotFound();
        }

        if (ForumContentHandler.UsesTopicComments(rule.Type) && (content.LevelNo ?? 0) > 0) return this.Redirect($"/content/{rule.RuleNo}/view/{content.TargetNo}?jump={content.ContentNo}"); // 포럼 댓글의 글번호: 소속 토픽을 열고 그 댓글이 있는 페이지로 이동 (토픽의 열람 조건은 토픽 화면이 확인)
        if (content.Properties.FirstOrDefault(p => p.Key == DefaultContentHandler.MovedToKey)?.Value is { } movedTo) return this.Redirect($"/content/view/{movedTo}"); // 다른 게시판으로 옮긴 글의 자리 글: 옮긴 글로 이동 (열람 조건은 그 글 화면이 확인)

        var model = this.PageModelOf(rule, PostView);
        if (!this.HttpContext.GetService<ICompositeViewEngine>()!.GetView(null, model.ViewPath, false).Success) return this.NotFound();
        var now = DateTime.UtcNow;
        var guestOwned = PostAccessRule.IsGuestOwned(content); // 작성자 정보 없이 비밀번호만 있는 비회원 글
        var passwordTried = !this.IsAuthenticated && guestOwned && password is not null;
        var passwordOk = passwordTried && ContentManager.PasswordMatches(content.Password, password); // 미리 입력받은 비밀번호가 맞으면 본인 글 (시도 제한은 화면 스크립트에서만 처리)
        this.ViewData["GuestPassword"] = passwordOk ? password : null; // 확인한 비회원 글 비밀번호: 보기 화면이 댓글·승인·변경 기록·수정 이동에 함께 보낸다

        // 읽기 권한 정리는 PostAccessRule 참고 (관리자·승인자·본인은 항상, 그 밖에는 읽기 권한·게시 조건·비공개·게시 대상)
        var access = PostAccessRule.Check(rule, model.Permission, content, this.CurrentUserAssets, passwordOk, now);
        if (access != PostAccessRule.Allowed)
        {
            if (passwordTried || (guestOwned && !this.IsAuthenticated)) return this.PostPasswordPrompt(model, content, passwordTried); // 비회원 글은 막힌 경우 비밀번호를 받아 본인 확인
            return access == PostAccessRule.NotFound ? this.NotFound() : this.Denied();
        }

        model.Post = await this.PostItemAsync(rule, model, content, passwordOk ? password : null);
        this.SetTitle(model.Post.Title);
        this.SetPageKey("Content");
        return rule.Type == ContentTypes.Blog ? this.View("Index", await this.SinglePostFeedAsync(rule, model, content)) : this.View("Index", model);
    }

    /// <summary>글 보기에 필요한 연관 값(태그·읽기 권한·속성·글쓴이 표시명/이미지)을 함께 읽는 글 조회식</summary>
    private IQueryable<ContentModel> PostQuery() => this.DbContext!.Contents.AsNoTracking().AsSplitQuery()
        .Include(p => p.Tags)
        .Include(p => p.ReadRule)
        .Include(p => p.Properties)
        .Include(p => p.Writer).ThenInclude(w => w!.DpNames)
        .Include(p => p.Writer).ThenInclude(w => w!.Properties.Where(x => x.Key == ContentManager.UserPhotoKey));

    /// <summary>
    /// 블로그 피드(<c>Blog/Index</c>)를 채웁니다: 목록 API 와 같은 처리(<see cref="ContentManager.ListPostsAsync"/> — 공개 조건·공지·검색·카테고리·승인 목록 모드, 블로그는 한 페이지 5개)로 이 쪽의 글번호를 받아,
    /// 열 수 있는 글은 글마다 글 보기 모델(<see cref="PostItemAsync"/>, 조회수도 올림)을 만들고 열 수 없는 글(잠금)·비회원 글(비밀번호 확인 필요)은 제목만 담습니다.
    /// 요청 쿼리: page(1부터), q(검색어), category, pending(1 이면 승인 목록 모드).
    /// </summary>
    /// <param name="rule">블로그 규칙</param>
    /// <param name="model">피드 페이지 모델 (<see cref="ContentPageModel.Feed"/> 와 쪽 정보를 채움)</param>
    private async Task FillFeedAsync(ContentRule rule, ContentPageModel model)
    {
        var values = ContentManager.ValuesOf(this.Request);
        values["ruleNo"] = rule.RuleNo.ToString();
        values["pending"] = this.Request.Query["pending"] == "1" ? "true" : "false";
        var res = await new ContentManager(this).ListPostsAsync(values);
        if (!res.Success) return;
        var data = System.Text.Json.JsonSerializer.SerializeToElement(res.Data);
        (model.Page, model.PageCount, model.Total, model.Pending, model.PendingCount) = (data.GetProperty("page").GetInt32(), data.GetProperty("pageCount").GetInt32(), data.GetProperty("total").GetInt32(), data.GetProperty("pending").GetBoolean(), data.GetProperty("pendingCount").GetInt32());
        var rows = data.GetProperty("items").EnumerateArray().Select(p => (No: p.GetProperty("contentNo").GetInt32(), Title: p.GetProperty("title").GetString() ?? string.Empty, Locked: p.GetProperty("locked").GetBoolean(), Gate: p.GetProperty("gate").GetBoolean(), Comments: p.GetProperty("commentCount").GetInt32())).ToList();
        var nos = rows.Where(p => !p.Locked && !p.Gate).Select(p => (int?)p.No).ToList();
        var contents = nos.Count == 0 ? [] : await this.PostQuery().Where(p => p.ContentRuleId == rule.Id && nos.Contains(p.ContentNo)).ToDictionaryAsync(p => p.ContentNo ?? 0);
        foreach (var row in rows)
        {
            var page = contents.TryGetValue(row.No, out var content) && PostAccessRule.Check(rule, model.Permission, content, this.CurrentUserAssets, false, DateTime.UtcNow) == PostAccessRule.Allowed ? this.PageModelOf(rule, PostView) : null;
            if (page is not null) (page.Post, page.InFeed, page.CommentCount) = (await this.PostItemAsync(rule, page, content!, null), true, row.Comments);
            model.Feed.Add((row.No, row.Title, row.Gate, page));
        }
    }

    /// <summary>
    /// 블로그 글 주소로 들어온 경우(통합 검색·알림·댓글 번호 등): 별도 보기 페이지 없이 그 글 하나만 담은 피드(<c>Blog/Index</c>)를 만듭니다. 댓글 수는 포럼형 댓글(글 행) 수입니다.
    /// </summary>
    /// <param name="rule">블로그 규칙</param>
    /// <param name="post">그 글의 글 보기 모델 (<see cref="ContentPageModel.Post"/> 를 채운 것)</param>
    /// <param name="content">그 글</param>
    /// <returns>글 하나만 담은 피드 모델</returns>
    private async Task<ContentPageModel> SinglePostFeedAsync(ContentRule rule, ContentPageModel post, ContentModel content)
    {
        var feed = this.PageModelOf(rule, ContentPageModel.IndexView);
        (post.InFeed, post.SinglePost, feed.SinglePost) = (true, true, true);
        post.CommentCount = await this.DbContext!.Contents.CountAsync(p => p.ContentRuleId == rule.Id && p.TargetNo == content.ContentNo && p.LevelNo > 0 && p.ContentNo != null);
        (feed.Page, feed.PageCount, feed.Total) = (1, 1, 1);
        feed.Feed.Add((post.Post!.ContentNo, post.Post.Title, false, post));
        return feed;
    }

    /// <summary>
    /// 글 보기 화면의 글 값을 만듭니다 (글 보기·블로그 피드 공통). 열람 검사를 통과한 글만 넘겨야 하며, 조회수를 올리고(중복 배제) 이전/다음 글·번역·첨부·권한별 버튼 값을 채웁니다.
    /// </summary>
    /// <param name="rule">컨텐츠 규칙</param>
    /// <param name="model">글 보기 페이지 모델 (권한·옵션·카테고리)</param>
    /// <param name="content">열람 검사를 통과한 글</param>
    /// <param name="password">확인한 비회원 글 비밀번호 (본인 확인을 안 했으면 null)</param>
    /// <returns>글 보기 값</returns>
    private async Task<ContentPostItem> PostItemAsync(ContentRule rule, ContentPageModel model, ContentModel content, string? password)
    {
        var db = this.DbContext!;
        var (now, isAdmin) = (DateTime.UtcNow, model.Permission.IsAdmin);
        var isWriter = PostAccessRule.IsWriter(content, this.CurrentUserId); // 회원 글의 글쓴이 본인
        var guestOwned = PostAccessRule.IsGuestOwned(content); // 작성자 정보 없이 비밀번호만 있는 비회원 글
        var passwordOk = password is not null;
        var approved = PostAccessRule.IsApproved(rule, content);
        await this.CountReadAsync(content); // 중복을 배제한 조회수 증가 (회원: 조회 기록, 비회원: 조회자 IP 속성)

        var publicQuery = this.PublicQuery(rule.Id, isAdmin, rule.Approvers.Count > 0, rule.Type == ContentTypes.Survey);
        // 이전/다음 글은 목록과 같은 순서(원본글 묶음 번호 내림차순, 같은 묶음은 답글 순서 오름차순)에서 바로 위(이전)·바로 아래(다음) 글이다
        var (target, sort) = (content.TargetNo ?? 0, content.SortNo ?? 0);
        var isForum = ForumContentHandler.UsesTopicComments(rule.Type); // 포럼 토픽·블로그 글 보기에는 이전/다음 글이 없음 (댓글도 글 행이라 이웃 글 계산에 섞임)
        var isManual = rule.Type == ContentTypes.Manual; // 메뉴얼은 목차 순서(그룹 → 그 아래 페이지 → 다음 그룹, 같은 카테고리 안)의 바로 앞·뒤 글
        var manualQuery = publicQuery.Where(p => isManual && p.ContentNo != null && p.Category == content.Category);
        var previous = isManual
            ? await NeighborAsync(sort > 0 ? manualQuery.Where(p => p.TargetNo == target && p.SortNo < sort).OrderByDescending(p => p.SortNo) : null, manualQuery.Where(p => p.TargetNo < target).OrderByDescending(p => p.TargetNo).ThenByDescending(p => p.SortNo))
            : isForum ? null : await NeighborAsync(sort > 0 ? publicQuery.Where(p => p.TargetNo == target && p.SortNo < sort).OrderByDescending(p => p.SortNo) : null, publicQuery.Where(p => p.TargetNo > target).OrderBy(p => p.TargetNo).ThenByDescending(p => p.SortNo));
        var next = isManual
            ? await NeighborAsync(manualQuery.Where(p => p.TargetNo == target && p.SortNo > sort).OrderBy(p => p.SortNo), manualQuery.Where(p => p.TargetNo > target).OrderBy(p => p.TargetNo).ThenBy(p => p.SortNo))
            : isForum ? null : await NeighborAsync(publicQuery.Where(p => p.TargetNo == target && p.SortNo > sort).OrderBy(p => p.SortNo), publicQuery.Where(p => p.TargetNo < target).OrderByDescending(p => p.TargetNo).ThenBy(p => p.SortNo));
        var anonymous = model.Option("UseAnonymous") && !isAdmin;
        var shown = await this.TranslatedAsync(rule, content); // 다국어 게시판: ?lang= 으로 고른 언어, 없으면 현재 언어 번역 → 원문
        var writer = anonymous ? default : (await ContentManager.ProfilesAsync((DbContext)db, this.Request, [content.WriterId])).GetValueOrDefault(content.WriterId ?? Guid.Empty); // 글쓴이 표시 이름·이미지·활성 그룹/역할
        var neighborTitles = DefaultContentHandler.SupportsTranslation(rule) ? await TranslationReader.TitlesAsync(db, this.Request, new[] { previous, next }.OfType<Neighbor>().Select(p => (p.Id, p.Culture))) : [];
        return new ContentPostItem
        {
            Id = content.Id,
            ContentNo = content.ContentNo ?? 0,
            Title = shown.Title,
            Body = rule.Type == ContentTypes.Survey ? content.ContentBody : EditorFile.Sanitize(shown.Body),
            Culture = shown.Culture,
            OriginalCulture = shown.Original,
            Translations = shown.Items, // 본문은 화면에 직접 출력하므로 현재 정제 규칙으로 한 번 더 정제 (설문 본문은 JSON 이라 제외)
            Category = model.Categories.FirstOrDefault(p => p.Key == content.Category).Value ?? content.Category ?? string.Empty,
            IsNotice = content.IsNotice,
            Writer = anonymous ? (this.Request.GetLanguage("Anonymous") ?? "Anonymous") : content.Writer is null ? string.Empty : this.Request.DisplayName(content.Writer.DpNames, content.Writer.UserName), // 글쓴이: 문화권에 맞는 표시명, 없으면 사용자 ID
            WriterId = anonymous ? null : content.WriterId, // 작성자 메뉴(메시지 보내기·글 보기)용 — 익명 게시판은 드러내지 않음
            WriterPhoto = writer.Photo, // 글쓴이 이미지 (없으면 기본 아이콘)
            WriterGroup = writer.Group,
            WriterRole = writer.Role,
            CDate = content.CDate,
            UDate = content.UDate,
            ReadCount = content.ReadCount + 1,
            Tags = [.. content.Tags.Select(p => p.Tag).OrderBy(p => p, StringComparer.CurrentCulture)],
            Files = await this.DownloadableFilesAsync(content.Id, passwordOk ? password : null, now),
            PreviousLocked = previous is not null && await this.IsLockedAsync(rule, model.Permission, previous.Id, now),
            NextLocked = next is not null && await this.IsLockedAsync(rule, model.Permission, next.Id, now),
            Links = ContentManager.ParseStrings(content.Properties.FirstOrDefault(p => p.Key == DefaultContentHandler.LinksKey)?.Value),
            IsPublished = content.Published,
            IsApproved = approved,
            IsRejected = rule.Approvers.Count > 0 && content.Approved == false,
            WriterIp = anonymous ? string.Empty : (content.Properties.FirstOrDefault(p => p.Key == DefaultContentHandler.WriterIpKey)?.Value ?? "127.0.0.1").MaskIp(), // 기록된 IP 가 없으면 로컬 IP
            Start = content.Start,
            End = content.End,
            HasApprovals = rule.Approvers.Count > 0 && (isAdmin || isWriter || PostAccessRule.IsApprover(rule, this.CurrentUserId)) && (content.Approved != true || DefaultContentHandler.ApprovalRecords(content).Count > 0), // 관리자·승인자·글쓴이에게만, 승인 대기·반려 글이거나 승인된 기록이 있을 때
            CanEdit = isAdmin || (DefaultContentHandler.CanWriteTo(model.Permission, content) && (model.Option("AllowEdit") || !content.Published) && (content.WriterId is null || isWriter)),
            CanDelete = isAdmin || (DefaultContentHandler.CanWriteTo(model.Permission, content) && model.Option("AllowDelete") && (isWriter || guestOwned)),
            CanReply = model.Permission.CanAnswer && model.Option("AllowAnswer") && content.Published && approved && !content.IsNotice && !isForum && (rule.Type != ContentTypes.Manual || (content.LevelNo ?? 0) == 0), // 게시·승인된 글에만 답글
            AddPageToNo = rule.Type == ContentTypes.Manual && model.Permission.CanAnswer && content.Published && approved && !content.IsNotice ? ((content.LevelNo ?? 0) == 0 ? content.ContentNo ?? 0 : content.TargetNo ?? 0) : 0,
            GroupNo = rule.Type == ContentTypes.Manual ? ((content.LevelNo ?? 0) == 0 ? content.ContentNo ?? 0 : content.TargetNo ?? 0) : 0,
            CanReorder = rule.Type == ContentTypes.Manual && (isAdmin || (this.CurrentUserId is { } me && ((content.LevelNo ?? 0) == 0 ? content.WriterId == me : await db.Contents.AnyAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == content.TargetNo && p.LevelNo == 0 && p.WriterId == me)))), // 관리자, 또는 그 그룹의 작성자 (그룹 보기뿐 아니라 그 그룹의 페이지 보기에서도)
            CanReport = this.IsAuthenticated && !isWriter && content.Published && approved,
            MoveTargets = DefaultContentHandler.CanMove(content, rule, isAdmin, this.CurrentUserId)
                ? [.. (await DefaultContentHandler.MoveTargetsAsync(db, rule, this.CurrentUserAssets, this.IsManager || this.IsAllowed(WebConfig.ContentManagerPolicyName))).Select(p => (p.RuleNo, this.Request.DisplayName(p.Title, p.RuleNo.ToString())))]
                : [],
            CanHistory = model.Option("UseHistory") && await db.ContentHistories.AnyAsync(p => p.ContentId == content.Id),
            NeedPassword = guestOwned && !isAdmin && !isWriter,
            LikePoint = content.LikePoint,
            Vote = (model.Option("UseLike") && this.CurrentUserId is { } likerId) ? await db.ContentLikers.Where(p => p.OwnerDataId == content.Id && p.LikeUserId == likerId).Select(p => p.Liked ? 1 : -1).FirstOrDefaultAsync() : 0,
            LevelNo = content.LevelNo ?? 0,
            PreviousNo = previous?.ContentNo ?? 0,
            PreviousTitle = previous is null ? string.Empty : neighborTitles.TryGetValue(previous.Id, out var previousShown) ? previousShown.Title : previous.Title,
            NextNo = next?.ContentNo ?? 0,
            NextTitle = next is null ? string.Empty : neighborTitles.TryGetValue(next.Id, out var nextShown) ? nextShown.Title : next.Title
        };
    }

    /// <summary>
    /// 다국어 게시판(<see cref="DefaultContentHandler.SupportsTranslation"/>)의 글 보기 언어를 정해 그 언어의 제목·본문을 돌려줍니다: 쿼리 <c>?lang=</c> 의 언어(원문 언어 또는 번역이 있는 언어)가 있으면 그 언어,
    /// 없으면 현재 언어의 번역, 그것도 없으면 원문(<see cref="TranslationReader.PickCulture"/>)입니다. 번역 목록(본문 제외)을 읽고 고른 번역의 본문만 한 건 더 읽습니다. 다국어 게시판이 아니면 원문 그대로(문화권 빈 값)입니다.
    /// </summary>
    private async Task<(string Title, string Body, string Culture, string Original, List<(string Culture, string Name, bool IsMachine, bool Outdated)> Items)> TranslatedAsync(ContentRule rule, ContentModel content)
    {
        if (!DefaultContentHandler.SupportsTranslation(rule)) return (content.Title, content.ContentBody, string.Empty, string.Empty, []);
        return await TranslationReader.ShownAsync(this.DbContext!, this.Request, content);
    }

    /// <summary>
    /// 글에 등록된 첨부 파일 중 현재 사용자가 내려받을 수 있는 것만 (다운로드 경로, "파일명 (용량)", 다운로드 횟수) 목록으로 만듭니다. 권한은 다운로드 경로와 같은 규칙(<see cref="FileDownload"/>)입니다.
    /// 비회원 글의 비밀번호를 맞춘 경우(<paramref name="password"/>)에는 본인 확인을 위해 경로에 비밀번호를 쿼리로 붙입니다.
    /// </summary>
    private async Task<List<(string Url, string Name, int DownCount, string InlineUrl)>> DownloadableFilesAsync(Guid contentId, string? password, DateTime now)
    {
        var db = this.DbContext!;
        var fileIds = await db.ContentFiles.AsNoTracking()
            .Where(m => m.ContentId == contentId && m.Registed)
            .Select(m => m.FileInfoId)
            .ToListAsync();
        if (fileIds.Count == 0) return [];

        var files = await db.Files.AsNoTracking().AsSplitQuery()
            .Include(p => p.Rule).ThenInclude(p => p!.AccessRules)
            .Include(p => p.Targets)
            .Where(p => fileIds.Contains(p.Id))
            .OrderBy(p => p.CDate)
            .ToListAsync();

        var query = password is null ? string.Empty : "?password=" + Uri.EscapeDataString(password);
        var result = new List<(string Url, string Name, int DownCount, string InlineUrl)>();
        foreach (var file in files)
            if (await FileDownload.CheckAsync(db, file, this.CurrentUserAssets, this.IsManager || this.IsAllowed(WebConfig.ContentManagerPolicyName), password, now) == FileDownload.Allowed)
                result.Add(($"/system/down/{file.Id}{query}", $"{(string.IsNullOrEmpty(file.FileExtension) ? file.FileName : $"{file.FileName}.{file.FileExtension}")} ({FileSizeText(file.FileSize)})", file.DownCount, GalleryContentHandler.IsWebImage(file.FileExtension, file.FileMime) ? $"/system/down/{file.Id}{(query.Length == 0 ? "?" : query + "&")}inline=true" : string.Empty));
        return result;
    }

    /// <summary>
    /// 이 사용자가 열 수 없는 글인지(이전/다음 글 표시용): 열 수 없으면 잠금 표시와 함께 링크를 막는다. 비회원이 쓴 글은 비밀번호 확인 화면으로 들어가야 하므로 잠기지 않은 것으로 본다.
    /// </summary>
    private async Task<bool> IsLockedAsync(ContentRule rule, ContentPermission permission, Guid contentId, DateTime now)
    {
        if (permission.IsAdmin || PostAccessRule.IsApprover(rule, this.CurrentUserId)) return false;
        var post = await this.DbContext!.Contents.AsNoTracking().AsSplitQuery()
            .Include(p => p.ReadRule)
            .Include(p => p.Properties)
            .FirstOrDefaultAsync(p => p.Id == contentId);
        if (post is null) return false;
        return PostAccessRule.Check(rule, permission, post, this.CurrentUserAssets, false, now) != PostAccessRule.Allowed && (this.IsAuthenticated || !PostAccessRule.IsGuestOwned(post));
    }

    /// <summary>파일 용량(byte)을 읽기 쉬운 글자로 바꿉니다 (예: 1536 → "1.5 KB")</summary>
    private static string FileSizeText(long bytes)
    {
        if (bytes <= 0) return "0 B";
        var i = Math.Min((int)Math.Floor(Math.Log(bytes, 1024)), 4);
        return $"{Math.Round(bytes / Math.Pow(1024, i), i == 0 ? 0 : 1)} {new[] { "B", "KB", "MB", "GB", "TB" }[i]}";
    }

    /// <summary>비회원 글의 본인 확인용 비밀번호 입력 화면 (<c>Default/PostPassword</c>)을 보여 줍니다. <paramref name="failed"/> 이면 비밀번호가 틀렸다는 안내가 함께 나옵니다.</summary>
    private IActionResult PostPasswordPrompt(ContentPageModel model, ContentModel content, bool failed)
    {
        (model.View, model.PostNo) = (PostPasswordView, content.ContentNo ?? 0);
        this.ViewData["PasswordFailed"] = failed;
        this.SetTitle(model.Title);
        this.SetPageKey("Content");
        return this.View("Index", model);
    }

    /// <summary>비회원 조회자 IP 를 쉼표로 이어 보관하는 글 속성 키</summary>
    private const string ReaderKey = "reader";

    /// <summary>비회원 조회자 IP 최대 보관 수 (넘으면 맨 앞부터 지움)</summary>
    private const int MaxReaderIps = 100;

    /// <summary>
    /// 글 조회수를 중복 없이 1 올립니다. 회원은 조회 기록(글+조회자)이 없을 때만 기록하고 올리며, 비회원은 글 속성 <c>reader</c> 의 조회자 IP 목록(쉼표 연결)에 없을 때만 IP 를 뒤에 덧붙이고 올립니다.
    /// IP 는 최대 100개만 보관하고 넘으면 맨 앞 항목부터 지웁니다. 비회원의 IP 를 알 수 없으면 중복을 가릴 수 없어 기록 없이 올립니다. 동시에 같은 조회가 겹쳐 기록이 이미 생겼으면 올리지 않습니다.
    /// </summary>
    /// <param name="content">조회한 글 (속성을 불러와 있어야 함)</param>
    private async Task CountReadAsync(ContentModel content)
    {
        var db = this.DbContext!;
        try
        {
            if (this.CurrentUserId is { } readerId)
            {
                if (await db.ContentReaders.AnyAsync(p => p.ContentId == content.Id && p.ReaderId == readerId)) return;
                db.ContentReaders.Add(new ContentReader { ContentId = content.Id, ReaderId = readerId });
                await ((DbContext)db).SaveChangesAsync();
            }
            else if (this.ClientIP is { Length: > 0 } ip)
            {
                var current = content.Properties.FirstOrDefault(p => p.Key == ReaderKey);
                var ips = (current?.Value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                if (ips.Contains(ip)) return;
                ips.Add(ip);
                if (ips.Count > MaxReaderIps) ips.RemoveRange(0, ips.Count - MaxReaderIps);
                var value = string.Join(',', ips);
                if (current is null)
                {
                    db.ContentProperties.Add(new Property<ContentModel, Guid>(content.Id, ReaderKey, value, isSystem: true));
                    await ((DbContext)db).SaveChangesAsync();
                }
                else
                {
                    var changedAt = DateTime.UtcNow;
                    await db.ContentProperties.Where(p => p.OwnerID == content.Id && p.Key == ReaderKey).ExecuteUpdateAsync(p => p.SetProperty(x => x.Value, value).SetProperty(x => x.UDate, changedAt).SetProperty(x => x.IsSystem, true));
                }
            }
        }
        catch (DbUpdateException) { return; } // 같은 조회자가 동시에 연 경우 먼저 들어간 기록을 유지하고 한 번만 셈
        await db.Contents.Where(p => p.Id == content.Id).ExecuteUpdateAsync(p => p.SetProperty(c => c.ReadCount, c => c.ReadCount + 1));
    }

    /// <summary>
    /// 규칙으로 컨텐츠 페이지 모델의 공통 부분(규칙 정보, 현재 사용자 권한, 상단/하단 HTML, 카테고리, 옵션, 사이드 구성)을 만듭니다. 규칙은 제목·접근 권한·속성을 불러와 있어야 합니다.
    /// </summary>
    private ContentPageModel PageModelOf(ContentRule rule, string view) => new ContentPageModel
    {
        RuleNo = rule.RuleNo,
        Title = this.Request.DisplayName(rule.Title, rule.RuleNo.ToString()),
        Type = rule.Type,
        View = view,
        Permission = this.PermissionOf(rule),
        TopHtml = rule.Properties.FirstOrDefault(p => p.Key == ContentPageModel.TopHtmlKey)?.Value ?? string.Empty,
        BottomHtml = rule.Properties.FirstOrDefault(p => p.Key == ContentPageModel.BottomHtmlKey)?.Value ?? string.Empty,
        Categories = ContentPageModel.ParseCategories(rule.Category),
        Options = ContentPageModel.ParseOptions(rule.Properties.FirstOrDefault(p => p.Key == ContentPageModel.OptionsKey)?.Value)
    }.WithSide(rule);

    /// <summary>
    /// 메뉴얼의 첫 글 번호: 요청 쿼리 <c>category</c>(없거나 규칙에 없으면 첫 카테고리, 카테고리가 없는 규칙은 전체)의 공개된 글 중 목차 순서(그룹 → 그 아래 페이지)로 가장 앞의 글 (없으면 null).
    /// </summary>
    private async Task<int?> FirstManualNoAsync(ContentRule rule, ContentPageModel model)
    {
        var category = model.Categories.Count == 0 ? null : model.Categories.Select(p => p.Key).FirstOrDefault(p => p == this.Request.Query["category"].ToString()) ?? model.Categories[0].Key;
        var topIds = await this.DbContext!.Contents.AsNoTracking().Where(p => p.ContentRuleId == rule.Id && p.ContentNo != null && p.Published && (category == null || p.Category == category))
            .OrderBy(p => p.TargetNo).ThenBy(p => p.SortNo).ThenBy(p => p.ContentNo).Take(200).Select(p => p.Id).ToListAsync();
        if (topIds.Count == 0) return null;
        var map = await this.DbContext!.Contents.AsNoTracking().Include(p => p.ReadRule).Include(p => p.Properties).Where(p => topIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var now = DateTime.UtcNow;
        foreach (var id in topIds)
            if (map.TryGetValue(id, out var p) && PostAccessRule.Check(rule, model.Permission, p, this.CurrentUserAssets, false, now) == PostAccessRule.Allowed) return p.ContentNo; // 열 수 있는지는 PostAccessRule 이 판정(관리자·승인자·본인의 미승인 글 포함, 게시 대상·비공개로 열 수 없는 글은 건너뜀)
        return null;
    }

    /// <summary>
    /// 목록 순서의 바로 이웃 글: 같은 묶음(TargetNo) 안에서 먼저 찾고, 없으면 이웃 묶음에서 찾습니다.
    /// 두 조건을 OR 로 묶은 쿼리 하나로 찾으면 인덱스 탐색을 못 해 오래된 글일수록 게시판 전체를 훑습니다.
    /// </summary>
    /// <param name="sameGroup">같은 묶음 안 후보 (정렬 포함)</param>
    /// <param name="otherGroups">이웃 묶음 후보 (정렬 포함)</param>
    /// <returns>이웃 글 (없으면 null)</returns>
    private static async Task<Neighbor?> NeighborAsync(IQueryable<ContentModel>? sameGroup, IQueryable<ContentModel> otherGroups)
        => (sameGroup is not null ? await sameGroup.Select(p => new Neighbor(p.Id, p.ContentNo, p.Title, p.Culture)).FirstOrDefaultAsync() : null)
            ?? await otherGroups.Select(p => new Neighbor(p.Id, p.ContentNo, p.Title, p.Culture)).FirstOrDefaultAsync();

    /// <summary>이전/다음 글 링크에 필요한 값 (원문 문화권은 다국어 게시판의 번역 제목용)</summary>
    private sealed record Neighbor(Guid Id, int? ContentNo, string Title, string? Culture);

    /// <summary>
    /// 공개된 글 조회식: 표시 + 게시 + 승인된 글이고 게시 기간 안인 글 (관리자는 규칙의 모든 글). 목록과 글 보기의 이전/다음 글이 함께 씁니다.
    /// </summary>
    private IQueryable<ContentModel> PublicQuery(Guid ruleId, bool isAdmin, bool needApproval, bool endless = false)
    {
        var now = DateTime.UtcNow;
        return this.DbContext!.Contents.AsNoTracking().Where(p => p.ContentRuleId == ruleId && (isAdmin || (p.Visible && p.Published && (!needApproval || p.Approved == true) && (p.Start == null || p.Start <= now) && (endless || p.End == null || p.End >= now))));
    }

    /// <summary>
    /// 게시판 설정: <c>/content/manage/{ruleNo}</c> 는 그 규칙의 설정, 규칙 번호가 없는 <c>/content/manage</c> 는 신규 등록(형식은 <c>?type=</c>, 생략하면 일반)입니다.
    /// 규칙 설정은 규칙 관리자(관리자 정책·컨텐츠 관리자 정책, 또는 규칙의 관리 대상), 신규 등록은 관리자 정책·컨텐츠 관리자 정책을 만족하는 사용자만 열 수 있고 아니면 비로그인 401, 로그인 403 입니다.
    /// 화면은 그 규칙 형식 폴더의 <c>Manage</c> 뷰(페이지 틀 <c>Views/Content/Index.cshtml</c> 안)이고 없는 형식이면 404 입니다. 비활성 규칙도 열 수 있습니다(다시 켜기 위해).
    /// </summary>
    /// <param name="ruleNo">컨텐츠 규칙 번호 (없으면 신규 등록)</param>
    /// <param name="type">신규 등록할 컨텐츠 형식 (규칙 번호가 없을 때만, 생략하면 <see cref="ContentTypes.Default"/>)</param>
    /// <returns>게시판 설정 뷰 결과 (모델: <see cref="ContentPageModel"/>, 신규면 규칙 번호 0)</returns>
    [HttpGet("/content/manage/{ruleNo:int?}")]
    public async Task<IActionResult> Manage(int? ruleNo, [FromQuery] string? type)
    {
        var (model, error) = await this.ManageModelAsync(ruleNo, type);
        if (error is not null) return error;
        this.SetTitle(model!.Title);
        this.SetPageKey("Content");
        return this.View("Index", model);
    }

    /// <summary>
    /// 관리자 콘솔의 게시판 설정: <c>/Manager/Content/Board/{ruleNo}</c> 는 그 규칙의 설정, 규칙 번호가 없으면 신규 등록(<c>?type=</c>)입니다.
    /// <see cref="Manage"/> 와 같은 권한·형식별 설정 파샬뷰를 관리자 레이아웃(<c>Views/Manager/Content/Board.cshtml</c>) 안에 보여 주고, 저장하면 관리자 컨텐츠 화면으로 돌아갑니다.
    /// </summary>
    /// <param name="ruleNo">컨텐츠 규칙 번호 (없으면 신규 등록)</param>
    /// <param name="type">신규 등록할 컨텐츠 형식 (규칙 번호가 없을 때만, 생략하면 <see cref="ContentTypes.Default"/>)</param>
    /// <returns>관리자 게시판 설정 뷰 결과 (모델: <see cref="ContentPageModel"/>)</returns>
    [HttpGet("/Manager/Content/Board/{ruleNo:int?}")]
    public async Task<IActionResult> ManagerBoard(int? ruleNo, [FromQuery] string? type)
    {
        var (model, error) = await this.ManageModelAsync(ruleNo, type);
        return error ?? this.View("~/Views/Manager/Content/Board.cshtml", model);
    }

    /// <summary>
    /// 게시판 설정 화면 모델을 만듭니다 (<see cref="Manage"/>·<see cref="ManagerBoard"/> 공통): 권한·규칙·형식 뷰를 확인해 실패하면 오류 응답을 돌려줍니다.
    /// </summary>
    /// <returns>(설정 모델, 오류 응답 — 성공이면 null)</returns>
    private async Task<(ContentPageModel? Model, IActionResult? Error)> ManageModelAsync(int? ruleNo, string? type)
    {
        ContentPageModel model;
        if (ruleNo is null)
        {
            if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return (null, this.Denied());
            if (!Enum.TryParse<ContentTypes>(type ?? nameof(ContentTypes.Default), true, out var newType) || !Enum.IsDefined(newType)) return (null, this.NotFound());
            model = new ContentPageModel
            {
                Type = newType,
                Title = string.Format(this.Request.GetLanguage("AddOf") ?? "Add {0}", this.Request.GetLanguage("ContentRule") ?? "Content rule"),
                Permission = new ContentPermission { IsAdmin = true, CanList = true, CanRead = true, CanWrite = true, CanAnswer = true, CanComment = true }
            };
        }
        else
        {
            var rule = await this.DbContext!.ContentRules.AsNoTracking().Include(p => p.Title).Include(p => p.AccessRule).Include(p => p.Properties).FirstOrDefaultAsync(p => p.RuleNo == ruleNo && !p.OnlyView);
            if (rule is null) return (null, this.NotFound());
            var permission = this.PermissionOf(rule);
            if (!permission.IsAdmin) return (null, this.Denied());
            model = new ContentPageModel { RuleNo = rule.RuleNo, Title = this.Request.DisplayName(rule.Title, rule.RuleNo.ToString()), Type = rule.Type, Permission = permission }.WithSide(rule); // 사이트 설정 화면도 그 게시판의 사이드 구성대로
        }

        model.View = ManageView;
        return this.HttpContext.GetService<ICompositeViewEngine>()!.GetView(null, model.ViewPath, false).Success ? (model, null) : (null, this.NotFound());
    }

    /// <summary>접근할 수 없을 때의 응답: 비로그인 401, 로그인 사용자 403 (오류 페이지)</summary>
    private IActionResult Denied() => this.StatusCode(this.IsAuthenticated ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized);

    /// <summary>현재 사용자의 규칙 권한 (관리자 정책 또는 컨텐츠 관리자 정책 충족 여부 포함)</summary>
    private ContentPermission PermissionOf(ContentRule rule)
        => ContentPermission.Resolve(rule, this.CurrentUserAssets, this.IsManager || this.IsAllowed(WebConfig.ContentManagerPolicyName));

    /// <summary>목록 파샬뷰 이름 (<c>/content/{ruleNo}/list</c>)</summary>
    private const string ListView = "List";

    /// <summary>설정(관리) 파샬뷰 이름 (<c>/content/manage/{ruleNo}</c>, 형식 폴더의 Manage 뷰, 규칙 관리자만)</summary>
    private const string ManageView = "Manage";

    /// <summary>쓰기 파샬뷰 이름 (<c>/content/{ruleNo}/write</c>)</summary>
    private const string WriteView = "Write";

    /// <summary>글 보기 파샬뷰 이름 (<c>/content/{ruleNo}/view/{contentNo}</c>, <c>/content/view/{contentKey}</c>)</summary>
    private const string PostView = "View";

    /// <summary>비회원 글 본인 확인(비밀번호 입력) 파샬뷰 이름 (주소로는 열 수 없고 글 보기에서만 쓰임)</summary>
    private const string PostPasswordView = "PostPassword";

    /// <summary>컨텐츠 파샬뷰 이름 형식: 영문으로 시작하는 영문·숫자·밑줄 32자 이내 (경로 조작 방지)</summary>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,31}$")]
    private static partial System.Text.RegularExpressions.Regex ViewNameRegex();
}
