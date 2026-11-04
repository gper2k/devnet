using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯 페이지의 배치를 읽어 그릴 영역 목록(<see cref="SectionView"/>)을 만듭니다 (요청 단위 서비스).
/// 영역 종류가 없어졌거나 꺼진 영역은 건너뛰고, 위젯 종류가 없어졌거나 꺼진 슬롯은 <see cref="SlotView.Missing"/> 으로 표시합니다.
/// 위젯마다 같은 키의 데이터 로더(<see cref="IWidgetLoader"/>)가 있으면 불러 결과를 담고, 로더가 실패하면 그 슬롯만 비우고 로그를 남깁니다.
/// ponytail: 요청마다 종류 테이블·로더를 다시 읽음(캐시 없음) — 방문이 많아지면 위젯별 CacheSeconds 로 결과 캐시를 붙일 것.
/// </summary>
public class PageSectionReader
{
    private readonly SiteDbContext db;
    private readonly WebConfig config;
    private readonly IWebHostEnvironment environment;
    private readonly IEnumerable<IWidgetLoader> loaders;
    private readonly ILogger<PageSectionReader> logger;
    private Dictionary<string, Data.Widget.SectionType>? sectionTypes;
    private Dictionary<string, Data.Widget.WidgetType>? widgetTypes;

    /// <summary>
    /// <see cref="PageSectionReader"/> 생성자.
    /// </summary>
    public PageSectionReader(SiteDbContext db, WebConfig config, IWebHostEnvironment environment, IEnumerable<IWidgetLoader> loaders, ILogger<PageSectionReader> logger)
    {
        this.db = db;
        this.config = config;
        this.environment = environment;
        this.loaders = loaders;
        this.logger = logger;
        this.sectionTypes = null;
        this.widgetTypes = null;
    }

    /// <summary>
    /// 페이지의 영역 목록을 만듭니다 (페이지 정의가 없으면 빈 목록).
    /// </summary>
    /// <param name="page">페이지 키 (home, portal …)</param>
    /// <param name="http">현재 요청</param>
    /// <param name="withData">위젯 데이터를 불러올지 (편집 정보만 필요하면 false)</param>
    public async Task<List<SectionView>> ReadAsync(string page, HttpContext http, bool withData = true)
    {
        if (PageLayouts.Definition(this.environment.ContentRootPath, page) is null) return [];
        var layout = await PageLayouts.LoadAsync(this.config, this.environment.ContentRootPath, page);
        var sectionTypes = this.sectionTypes ??= await this.db.SectionTypes.AsNoTracking().Where(p => p.Available && p.Enabler).ToDictionaryAsync(p => p.Key);
        var widgetTypes = this.widgetTypes ??= await this.db.WidgetTypes.AsNoTracking().ToDictionaryAsync(p => p.Key);
        var result = new List<SectionView>();
        foreach (var section in PageLayouts.Sections(layout))
        {
            var id = section["id"]?.GetValue<string>() ?? string.Empty;
            if (!WidgetCatalog.KeyPattern().IsMatch(id) || !sectionTypes.TryGetValue(section["type"]?.GetValue<string>() ?? string.Empty, out var type)) continue;
            var areas = SectionGrid.Areas(JsonNode.Parse(type.Layout) as JsonObject ?? [], section["repeat"]?.GetValue<int>());
            var view = new SectionView { Id = id, ElementId = $"ws-{page.Replace(PageLayouts.VariantSeparator, '-')}-{id}", Type = type.Key, TypeName = CultureText.Of(http.Request, type.Name, type.Key)! };
            view.Css = SectionGrid.Css(view.ElementId, areas);
            foreach (var key in SectionGrid.SlotKeys(areas))
            {
                var (width, height) = SectionGrid.SlotSize(areas, key);
                var slot = new SlotView { Key = key, Width = width, Height = height };
                if (section["slots"]?[key] is JsonObject placed) await this.FillAsync(slot, placed, widgetTypes, page, http, withData);
                view.Slots.Add(slot);
            }
            result.Add(view);
        }
        return result;
    }

    /// <summary>슬롯에 놓인 위젯 정보를 채웁니다: 종류 확인, 제목, 설정값(기본값 채움), 데이터 로더 결과.</summary>
    private async Task FillAsync(SlotView slot, JsonObject placed, Dictionary<string, Data.Widget.WidgetType> widgetTypes, string page, HttpContext http, bool withData)
    {
        slot.HttpContext = http;
        slot.Widget = placed["widget"]?.GetValue<string>();
        if (slot.Widget is null) return;
        if (!widgetTypes.TryGetValue(slot.Widget, out var type) || !type.Available || !type.Enabler)
        {
            slot.Missing = true;
            return;
        }
        slot.Frame = WidgetCatalog.Flags(this.environment.ContentRootPath, type.Key).Frame;
        slot.Title = slot.Frame ? CultureText.Of(http.Request, placed["title"]) : null; // 틀 없는 위젯은 제목 줄도 없음
        slot.Settings = WidgetSettings.Normalize(type.Settings, placed["settings"] as JsonObject);
        if (!withData || this.loaders.FirstOrDefault(p => p.Widget == type.Key) is not { } loader) return;
        try
        {
            var context = new WidgetContext(page, slot.Settings, http);
            slot.Data = await loader.LoadAsync(context, http.RequestAborted);
            slot.MoreUrl = context.MoreUrl;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, "Widget loader failed [{Widget}] on page [{Page}]", type.Key, page);
            slot.Missing = true;
        }
    }
}
