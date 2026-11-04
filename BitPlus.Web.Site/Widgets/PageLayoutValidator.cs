using BitPlus.Web.Site.Data.Widget;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 편집 화면이 보낸 페이지 배치를 검사하고 정리합니다: 영역 id 형식·중복, 영역 종류(있고 켜짐), 가로형 칸 수 범위, 슬롯 키,
/// 위젯 종류(있고 켜짐, 페이지 카테고리, 슬롯 크기가 위젯 최소~최대 안), 제목(문화권별 100자), 설정값(<see cref="WidgetSettings.Normalize"/>).
/// 알 수 없는 속성은 버린 새 배치를 만들어 돌려줍니다.
/// </summary>
public static class PageLayoutValidator
{
    /// <summary>
    /// 배치를 검사해 정리된 배치를 돌려줍니다. 틀리면 Layout = null 과 문제가 된 항목(영역 id·위젯 키)을 돌려줍니다.
    /// </summary>
    /// <param name="input">보내온 배치</param>
    /// <param name="categories">페이지가 쓸 수 있는 위젯 카테고리</param>
    /// <param name="sectionTypes">영역 종류 (키별)</param>
    /// <param name="widgetTypes">위젯 종류 (키별)</param>
    /// <param name="isSingle">한 배치에 하나만 놓을 수 있는 위젯인지 (widget.json <c>single</c>, 없으면 제한 없음) — 두 번째부터는 틀린 배치</param>
    public static (JsonObject? Layout, string? Problem) Validate(JsonObject input, string[] categories, Dictionary<string, SectionType> sectionTypes, Dictionary<string, WidgetType> widgetTypes, Func<string, bool>? isSingle = null)
    {
        var sections = new JsonArray();
        var ids = new HashSet<string>();
        var singles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in PageLayouts.Sections(input))
        {
            var id = section["id"]?.GetValue<string>() ?? string.Empty;
            var typeKey = section["type"]?.GetValue<string>() ?? string.Empty;
            if (!WidgetCatalog.KeyPattern().IsMatch(id) || !ids.Add(id)) return (null, id);
            if (!sectionTypes.TryGetValue(typeKey, out var type) || !type.Available || !type.Enabler) return (null, typeKey);

            var definition = JsonNode.Parse(type.Layout) as JsonObject ?? [];
            int? repeat = definition["repeat"] is JsonObject range ? SectionGrid.RepeatOf(range, section["repeat"]?.GetValue<int>()) : null;
            var areas = SectionGrid.Areas(definition, repeat);
            var slots = new JsonObject();
            foreach (var key in SectionGrid.SlotKeys(areas))
            {
                if (section["slots"]?[key] is not JsonObject placed || placed["widget"]?.GetValue<string>() is not { Length: > 0 } widgetKey) continue;
                var (width, height) = SectionGrid.SlotSize(areas, key);
                if (!widgetTypes.TryGetValue(widgetKey, out var widget) || !Fits(widget, categories, width, height)) return (null, widgetKey);
                if (isSingle?.Invoke(widget.Key) == true && !singles.Add(widget.Key)) return (null, widgetKey);
                slots[key] = new JsonObject
                {
                    ["widget"] = widget.Key,
                    ["title"] = TitleOf(placed["title"] as JsonObject),
                    ["settings"] = WidgetSettings.Normalize(widget.Settings, placed["settings"] as JsonObject)
                };
            }
            var clean = new JsonObject { ["id"] = id, ["type"] = type.Key, ["slots"] = slots };
            if (repeat is { } count) clean["repeat"] = count;
            sections.Add(clean);
        }
        var result = new JsonObject { ["sections"] = sections };
        if (input["version"]?.GetValue<int>() is { } v) result["version"] = v;
        if (input["customized"]?.GetValue<bool>() is { } c) result["customized"] = c;
        return (result, null);
    }

    /// <summary>
    /// 위젯을 이 페이지의 이 크기 슬롯에 넣을 수 있는지: 켜져 있고, 페이지 카테고리에 들고, 슬롯 크기가 위젯 최소~최대 안.
    /// </summary>
    /// <param name="widget">위젯 종류</param>
    /// <param name="categories">페이지 카테고리</param>
    /// <param name="width">슬롯 가로 (칸 수)</param>
    /// <param name="height">슬롯 세로 (행 수)</param>
    public static bool Fits(WidgetType widget, string[] categories, double width, int height)
        => widget.Available && widget.Enabler && PageLayouts.Allows(categories, widget.Categories)
           && width + 0.001 >= widget.MinWidth && width <= widget.MaxWidth + 0.001 && height >= widget.MinHeight && height <= widget.MaxHeight;

    /// <summary>제목 — 문화권 키(영문 2~10자)별 100자, 빈 값은 버림</summary>
    private static JsonObject TitleOf(JsonObject? title)
    {
        var result = new JsonObject();
        foreach (var (culture, value) in title ?? [])
            if (culture.Length is >= 2 and <= 10 && value is JsonValue v && v.TryGetValue<string>(out var text) && text.Trim().Length > 0) result[culture] = text.Trim().Clip(100);
        return result;
    }
}
