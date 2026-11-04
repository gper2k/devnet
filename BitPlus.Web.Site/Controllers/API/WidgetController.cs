using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Site.Data;
using BitPlus.Web.Site.Widgets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 위젯 페이지 편집 Web API 컨트롤러 (api/widget). 페이지 위 편집 모드가 영역·위젯 목록을 받고 배치를 저장합니다. 관리자 정책만.
/// </summary>
public class WidgetController : APIControllerBase
{
    /// <summary>
    /// <see cref="WidgetController"/> 기본 생성자.
    /// </summary>
    public WidgetController() : base() { }

    /// <summary>사이트 DB</summary>
    private SiteDbContext Db => (SiteDbContext)this.DbContext!;

    /// <summary>
    /// 편집에 필요한 목록: 지금 배치, 쓸 수 있는 영역 종류(순서대로, 배치 정의 포함), 이 페이지에서 쓸 수 있는 위젯 종류(카테고리, 크기, 설정 항목).
    /// </summary>
    /// <param name="page">페이지 키 (home, portal …)</param>
    /// <returns>layout, sections([{key, name, description, layout}]), widgets([{key, name, description, minWidth, minHeight, maxWidth, maxHeight, settings, single(한 배치에 하나만), frame(공통 카드 틀·제목 사용)}])</returns>
    [HttpGet("catalog")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> Catalog([FromQuery] string? page)
    {
        var root = this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        if (PageLayouts.Definition(root, page ?? string.Empty) is not { } definition) return this.NotFoundResult(WebResources.NotFoundData);
        var categories = PageLayouts.Categories(definition);
        var layout = await PageLayouts.LoadAsync(this.Config!, root, page!);
        var sections = (await this.Db.SectionTypes.AsNoTracking().Where(p => p.Available && p.Enabler).OrderBy(p => p.SortOrder).ThenBy(p => p.Key).ToListAsync())
            .Select(p => new { key = p.Key, name = CultureText.Of(this.Request, p.Name, p.Key), description = CultureText.Of(this.Request, p.Description), layout = JsonNode.Parse(p.Layout) });
        var widgets = (await this.Db.WidgetTypes.AsNoTracking().Where(p => p.Available && p.Enabler).OrderBy(p => p.Key).ToListAsync())
            .Where(p => PageLayouts.Allows(categories, p.Categories))
            .Select(p => new { key = p.Key, name = CultureText.Of(this.Request, p.Name, p.Key), description = CultureText.Of(this.Request, p.Description), minWidth = p.MinWidth, minHeight = p.MinHeight, maxWidth = p.MaxWidth, maxHeight = p.MaxHeight, settings = JsonNode.Parse(p.Settings), single = WidgetCatalog.Flags(root, p.Key).Single, frame = WidgetCatalog.Flags(root, p.Key).Frame });
        return this.Complete(new { layout, sections, widgets });
    }

    /// <summary>
    /// 페이지 배치를 검사해 저장합니다 (<see cref="PageLayoutValidator"/> — 영역·위젯 종류, 카테고리, 크기, 설정값). 정리된 배치를 설정 <c>PageLayout.{페이지}</c> 에 저장합니다.
    /// </summary>
    /// <param name="page">페이지 키</param>
    /// <param name="layout">배치 JSON 문자열</param>
    /// <returns>처리 결과</returns>
    [HttpPut("layout")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveLayout([FromForm] string? page, [FromForm] string? layout)
    {
        var root = this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        if (PageLayouts.Definition(root, page ?? string.Empty) is not { } definition) return this.NotFoundResult(WebResources.NotFoundData);
        JsonObject? input;
        try { input = JsonNode.Parse(layout ?? string.Empty) as JsonObject; }
        catch (System.Text.Json.JsonException) { input = null; }
        if (input is null) return this.Error(WebResources.ValidRegex.Formats(this.Lang("WidgetLayout"))!);

        var sectionTypes = await this.Db.SectionTypes.AsNoTracking().ToDictionaryAsync(p => p.Key);
        var widgetTypes = await this.Db.WidgetTypes.AsNoTracking().ToDictionaryAsync(p => p.Key);
        var (clean, problem) = PageLayoutValidator.Validate(input, PageLayouts.Categories(definition), sectionTypes, widgetTypes, key => WidgetCatalog.Flags(root, key).Single);
        if (clean is null) return this.Error(this.Lang("WidgetLayoutInvalid").Formats(problem ?? string.Empty)!);
        var defVersion = definition["version"]?.GetValue<int>() ?? (definition["layout"] as JsonObject)?["version"]?.GetValue<int>() ?? 1;
        clean["version"] = Math.Max(defVersion, input["version"]?.GetValue<int>() ?? defVersion);
        clean["customized"] = true;
        await PageLayouts.SaveAsync(this.Config!, page!, clean);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 변형 페이지(<c>{페이지}@{변형}</c>, 예: 사이드 연결 메뉴별 <c>side@UserContent</c>)의 따로 저장한 배치를 지워 앞 페이지(전역) 배치로 되돌립니다. 변형이 아닌 페이지는 지울 수 없습니다.
    /// </summary>
    /// <param name="page">변형 페이지 키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("layout")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> ResetLayout([FromQuery] string? page)
    {
        var root = this.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        if (PageLayouts.Definition(root, page ?? string.Empty) is null || PageLayouts.BaseOf(page!) == page) return this.NotFoundResult(WebResources.NotFoundData);
        await PageLayouts.ResetAsync(this.Config!, this.Db, page!);
        return this.Complete(message: WebResources.DeletedComplete);
    }
}
