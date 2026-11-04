using BitPlus.Data.Account;
using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.HTTP.View;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 보기 전용(OnlyView) 컨텐츠 규칙에 등록된 글 하나를 바로 출력하는 MVC 뷰 컨트롤러입니다. 컨텐츠 등록만으로 사이트 페이지를 늘려 가는 기능입니다. 뷰는 <c>Views/View</c> 폴더에 있습니다.
/// </summary>
public class ViewController : ViewControllerBase
{
    /// <summary>
    /// <see cref="ViewController"/> 기본 생성자.
    /// </summary>
    public ViewController() : base() { }

    /// <summary>
    /// 컨텐츠 보기: <c>/view/{contentNo}</c> — 보기 전용(OnlyView) 컨텐츠 규칙(사이트에 하나만 허용)의 글 중 글 번호가 contentNo 인 글의 본문만 출력합니다 (제목은 페이지 제목으로만 씀).
    /// 보기 전용 규칙이 없거나 비활성이거나, 글이 없거나 공개 조건(표시 + 게시, 게시 기간 안)에 맞지 않으면 404 입니다. 관리자 정책(<see cref="WebConfig.ManagerPolicyName"/>)을 만족하는 사용자는 게시 기간과 읽기 권한에 상관없이 볼 수 있습니다.
    /// 글에 읽기 권한(ReadRule)이 있으면 그 대상(사용자·그룹·역할)에 속한 로그인 사용자와 관리자만 볼 수 있고, 아니면 403 입니다(비어 있으면 모두 읽기 가능).
    /// 컨텐츠는 관리자가 관리하므로 본문(HTML)은 정제·샌드박스 없이 그대로 출력합니다. 글 속성의 사이드 구성(감춤·위치·연결 메뉴, 기본 감춤)대로 사이드 패널을 함께 보여 줍니다.
    /// ponytail: 규칙 옵션(속성 Property)에 따른 승인 검사, 규칙 단위 읽기 권한(AccessRule), 조회수 증가는 아직 처리하지 않음 — 컨텐츠 본문 기능과 함께 추가
    /// </summary>
    /// <param name="contentNo">보기 전용 규칙 안의 글 번호</param>
    /// <returns>컨텐츠 보기 뷰 결과 (모델: <see cref="ContentViewModel"/>)</returns>
    [HttpGet("/view/{contentNo:int}")]
    public async Task<IActionResult> Index(int contentNo)
    {
        var db = this.DbContext!;
        var rule = await db.ContentRules.AsNoTracking().FirstOrDefaultAsync(p => p.OnlyView && p.Enabler);
        if (rule is null) return this.NotFound();

        var (now, isManager) = (DateTime.UtcNow, this.IsManager);
        var content = await db.Contents.AsNoTracking().Include(p => p.ReadRule).Include(p => p.Properties).FirstOrDefaultAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == contentNo && p.Visible && p.Published
            && (isManager || ((p.Start == null || p.Start <= now) && (p.End == null || p.End >= now))));
        if (content is null) return this.NotFound();
        if (!isManager && !this.CanRead(content)) return this.StatusCode(StatusCodes.Status403Forbidden);

        var shown = await TranslationReader.ShownAsync(db, this.Request, content); // 다국어: ?lang= 으로 고른 언어, 없으면 현재 언어 번역 → 원문 (보기 전용 페이지는 항상 다국어)
        this.SetTitle(shown.Title);
        this.SetPageKey("View");
        return this.View(new ContentViewModel
        {
            RuleNo = rule.RuleNo, ContentNo = content.ContentNo ?? 0, Title = shown.Title, Body = shown.Body, CDate = content.CDate, CanEdit = isManager,
            Culture = shown.Culture, OriginalCulture = shown.Original, Translations = shown.Items,
            SideHidden = PageViewContentHandler.SideHidden(content),
            SideRight = content.Properties.Any(p => p.Key == DefaultContentHandler.SidePositionKey && p.Value == DefaultContentHandler.SideRight),
            SideMenuKey = content.Properties.FirstOrDefault(p => p.Key == DefaultContentHandler.SideMenuKey)?.Value
        });
    }

    /// <summary>
    /// 현재 사용자가 글의 읽기 권한 대상에 속하는지 확인합니다 (읽기 권한이 비어 있으면 모두 허용, 있으면 로그인한 사용자 본인·소속 그룹(Group 직접 소속, Groups 상위 그룹 포함)·역할(그룹을 통해 얻은 역할 포함) 중 하나가 대상일 때만).
    /// </summary>
    private bool CanRead(ContentModel content)
    {
        if (content.ReadRule.Count == 0) return true;
        var user = this.CurrentUserAssets;
        if (user is null) return false;
        return content.ReadRule.Any(p => ContentPermission.Matches(p.TargetType, p.TargetKey, user));
    }

    /// <summary>
    /// 컨텐츠 작성/수정 페이지: <c>/view/edit</c> 는 신규 작성, <c>/view/edit/{글 번호}</c> 는 그 글 수정입니다. 관리자 전용 정책(<see cref="WebConfig.ManagerPolicyName"/>)만 열 수 있고 아니면 권한 없음 페이지입니다.
    /// 보기 전용 규칙이 없거나 비활성이면, 글이 없으면(수정) 404 입니다. 저장은 <c>api/content/post</c>(type=PageView) 입니다.
    /// </summary>
    /// <param name="contentNo">수정할 글 번호 (없으면 신규 작성)</param>
    /// <returns>컨텐츠 작성/수정 뷰 결과 (모델: <see cref="ContentEditModel"/>)</returns>
    [HttpGet("/view/edit/{contentNo:int?}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> Edit(int? contentNo)
    {
        var model = await this.EditModelAsync(contentNo);
        if (model is null) return this.NotFound();
        this.SetTitle(model.Title);
        this.SetPageKey("View");
        return this.View(model);
    }

    /// <summary>
    /// 관리자 콘솔의 페이지 컨텐츠 작성/수정: <c>/Manager/Content/Page</c> 는 신규, <c>/Manager/Content/Page/{글 번호}</c> 는 수정입니다. <see cref="Edit"/> 와 같은 폼(파샬뷰 <c>Views/View/EditForm.cshtml</c>)을
    /// 관리자 레이아웃(<c>Views/Manager/Content/Page.cshtml</c>) 안에 보여 주고, 저장·취소·삭제하면 관리자 컨텐츠 화면의 페이지 컨텐츠 탭으로 돌아갑니다. 관리자 정책만 열 수 있습니다.
    /// </summary>
    /// <param name="contentNo">수정할 글 번호 (없으면 신규 작성)</param>
    /// <returns>관리자 페이지 컨텐츠 편집 뷰 결과 (모델: <see cref="ContentEditModel"/>)</returns>
    [HttpGet("/Manager/Content/Page/{contentNo:int?}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> ManagerPage(int? contentNo)
    {
        var model = await this.EditModelAsync(contentNo);
        return model is null ? this.NotFound() : this.View("~/Views/Manager/Content/Page.cshtml", model);
    }

    /// <summary>
    /// 작성/수정 화면 모델을 만듭니다 (<see cref="Edit"/>·<see cref="ManagerPage"/> 공통). 보기 전용 규칙이 없거나 비활성이거나, 수정할 글이 없으면 null 입니다.
    /// </summary>
    private async Task<ContentEditModel?> EditModelAsync(int? contentNo)
    {
        if (contentNo < 0) return null;

        var db = this.DbContext!;
        var rule = await db.ContentRules.AsNoTracking().FirstOrDefaultAsync(p => p.OnlyView && p.Enabler);
        if (rule is null) return null;

        var model = new ContentEditModel { ContentNo = contentNo ?? ContentEditModel.NewNo, OwnerKey = Guid.NewGuid().ToString() };
        if (model.IsNew) return model;
        var content = await db.Contents.AsNoTracking().FirstOrDefaultAsync(p => p.ContentRuleId == rule.Id && p.ContentNo == model.ContentNo);
        if (content is null) return null;
        (model.OwnerKey, model.Title, model.Body) = (content.Id.ToString(), content.Title, content.ContentBody);
        return model;
    }
}
