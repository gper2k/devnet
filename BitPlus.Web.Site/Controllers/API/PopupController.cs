using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.Security.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "시스템 설정 > 팝업" 화면용 Web API 컨트롤러 (api/popup). 사이트 팝업 목록(등록 최신순)·상세·등록/수정·삭제를 제공하며 관리자 정책만 쓸 수 있습니다.
/// 팝업이 바뀌면 사용자별 팝업 캐시(<see cref="PopupReader"/>)를 모두 지워 다음 페이지 로드부터 새 팝업이 반영되게 합니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public class PopupController : APIControllerBase
{
    /// <summary>팝업 본문 에디터 이미지의 소유자 형식 (소유자 키 = 팝업 식별키)</summary>
    public const string OwnerType = "site.popup";

    /// <summary>
    /// <see cref="PopupController"/> 기본 생성자.
    /// </summary>
    public PopupController() : base() { }

    /// <summary>
    /// 팝업 목록을 등록 최신순으로 페이지 단위 조회합니다.
    /// </summary>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items([{id, title, start, end, width, height, enabler, targetCount, cDate}]), total, page, pageSize</returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = this.DbContext!.Popups.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { id = p.Id, title = p.Title, area = p.Area.ToString(), start = p.Start, end = p.End, width = p.Width, height = p.Height, enabler = p.Enabler, targetCount = p.Targets.Count, cDate = p.CDate })
            .ToListAsync();
        return this.Complete(new { items, total, page, pageSize });
    }

    /// <summary>
    /// 팝업 하나를 조회합니다 (편집 모달 채우기용, 게시 대상 표시명 포함).
    /// </summary>
    /// <param name="id">팝업 식별키</param>
    /// <returns>id, title, body, start, end, width, height, enabler, targets([{type, key, name}])</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var popup = await this.DbContext!.Popups.AsNoTracking().Include(p => p.Targets).FirstOrDefaultAsync(p => p.Id == id);
        if (popup is null) return this.NotFoundResult(WebResources.NotFoundData);
        var names = await this.TargetNamesAsync(popup.Targets.Select(p => (p.TargetType, p.TargetKey)));
        return this.Complete(new
        {
            id = popup.Id, title = popup.Title, area = popup.Area.ToString(), body = popup.Body, start = popup.Start, end = popup.End, width = popup.Width, height = popup.Height, enabler = popup.Enabler,
            targets = popup.Targets.Select(p => new { type = p.TargetType.ToString(), key = p.TargetKey, name = names.GetValueOrDefault((p.TargetType, p.TargetKey)) ?? p.TargetKey.ToString() })
        });
    }

    /// <summary>
    /// 팝업을 등록하거나 수정합니다 (식별키가 있는 팝업이면 수정, 없으면 그 식별키로 새로 등록 — 화면이 미리 만든 식별키로 에디터 이미지를 올리기 때문).
    /// 본문은 정제해 저장하고 본문이 참조하지 않는 에디터 이미지는 지웁니다.
    /// </summary>
    /// <param name="id">팝업 식별키</param>
    /// <param name="title">제목 (필수, 256자 이내)</param>
    /// <param name="area">띄울 화면 영역 (User 사용자 / Manager 관리자 / Account 인증)</param>
    /// <param name="body">본문 HTML</param>
    /// <param name="start">게시 시작일 (ISO 8601 UTC, 필수)</param>
    /// <param name="end">게시 종료일 (ISO 8601 UTC, 필수, 시작일 이후)</param>
    /// <param name="targets">게시 대상 JSON 배열 [{type, key}] (비우면 모든 사용자)</param>
    /// <param name="width">너비 px (100~2000)</param>
    /// <param name="height">높이 px (0 이면 내용 높이, 최대 2000)</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>저장된 팝업 식별키</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromForm] Guid id, [FromForm] string? title, [FromForm] string? area, [FromForm] string? body, [FromForm] string? start, [FromForm] string? end, [FromForm] string? targets, [FromForm] int width, [FromForm] int height, [FromForm] bool enabler)
    {
        title = title?.Trim();
        if (id == Guid.Empty) return this.Error(WebResources.ValidRequired.Formats("id")!);
        if (string.IsNullOrEmpty(title) || title.Length > 256) return this.Error(WebResources.ValidRequired.Formats(this.Lang("ContentTitle"))!);
        if (!Enum.TryParse<PopupArea>(area, true, out var popupArea) || !Enum.IsDefined(popupArea)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("PopupArea"))!);
        if (width is < 100 or > 2000 || height is < 0 or > 2000) return this.Error(WebResources.ValidRegex.Formats(this.Lang("PopupSize"))!);
        var (from, to) = (ContentManager.ParseUtc(start), ContentManager.ParseUtc(end));
        if (from is null || to is null) return this.Error(WebResources.ValidRequired.Formats(this.Lang("PublishPeriod"))!); // 팝업은 게시 기간(시작·종료) 필수
        if (to < from) return this.Error(this.Lang("PeriodInvalid"));

        var db = this.DbContext!;
        var popup = await db.Popups.Include(p => p.Targets).FirstOrDefaultAsync(p => p.Id == id);
        var isNew = popup is null;
        popup ??= db.Popups.Add(new SitePopup { Id = id, Title = title, Body = string.Empty }).Entity;
        popup.Area = popupArea;
        (popup.Title, popup.Body, popup.Start, popup.End, popup.Width, popup.Height, popup.Enabler) = (title, EditorFile.Sanitize(body), from, to, width, height, enabler);
        var picked = ParseTargets(targets);
        popup.Targets.RemoveAll(p => !picked.Contains((p.TargetType, p.TargetKey)));
        foreach (var (type, key) in picked)
            if (!popup.Targets.Any(p => p.TargetType == type && p.TargetKey == key)) popup.Targets.Add(new SitePopupTarget { PopupId = popup.Id, TargetType = type, TargetKey = key });
        if (!isNew) popup.UDate = DateTime.UtcNow;
        await ((DbContext)db).SaveChangesAsync();

        await EditorFile.SyncAsync(db, this.Config!.UploadFolder, OwnerType, popup.Id.ToString(), popup.Body, false);
        await PopupReader.ClearAsync();
        return this.Complete(new { id = popup.Id }, isNew ? WebResources.AddedComplete : WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 팝업의 활성 여부만 바꿉니다 (목록의 활성 토글).
    /// </summary>
    /// <param name="id">팝업 식별키</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("{id:guid}/enabler")]
    public async Task<IActionResult> SetEnabler(Guid id, [FromForm] bool enabler)
    {
        var db = this.DbContext!;
        var popup = await db.Popups.FirstOrDefaultAsync(p => p.Id == id);
        if (popup is null) return this.NotFoundResult(WebResources.NotFoundData);
        (popup.Enabler, popup.UDate) = (enabler, DateTime.UtcNow);
        await ((DbContext)db).SaveChangesAsync();
        await PopupReader.ClearAsync();
        return this.Complete(); // 목록 토글은 조용히 바뀌므로 완료 안내 문구를 보내지 않는다 (문구가 있으면 화면이 알림을 띄움)
    }

    /// <summary>
    /// 팝업을 삭제합니다 (게시 대상과 본문 에디터 이미지 포함).
    /// </summary>
    /// <param name="id">팝업 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var db = this.DbContext!;
        var popup = await db.Popups.FirstOrDefaultAsync(p => p.Id == id);
        if (popup is null) return this.NotFoundResult(WebResources.NotFoundData);
        db.Popups.Remove(popup);
        await ((DbContext)db).SaveChangesAsync();
        await EditorFile.DeleteOwnerAsync(db, this.Config!.UploadFolder, OwnerType, id.ToString());
        await PopupReader.ClearAsync();
        return this.Complete(null, WebResources.DeletedComplete);
    }
}
