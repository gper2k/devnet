using System.Text;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 영역(Section) 격자 계산: section.json 의 배치 정의(<c>slots</c> + <c>layouts</c>, 또는 가로형 <c>repeat</c>)에서
/// 화면 폭 4단계별 CSS grid 영역 줄, 슬롯 목록, 슬롯 크기(가로 4칸 격자 기준)를 구하고 CSS 를 만듭니다. 화면과 썸네일이 같은 정의를 씁니다.
/// </summary>
public static class SectionGrid
{
    /// <summary>화면 폭 단계 키 (1024 이상 · 1024 미만 · 780 미만 · 520 미만)</summary>
    public static readonly string[] Breakpoints = ["1024", "1023", "779", "519"];

    /// <summary>
    /// 화면 폭 단계별 영역 줄을 돌려줍니다. 가로형(<c>repeat</c>)은 칸 수에 맞춰 만들고(780 미만 2칸, 520 미만 1칸씩 줄바꿈, 마지막 줄이 모자라면 마지막 칸이 남은 폭을 채움), 그 밖에는 정의의 layouts 그대로.
    /// </summary>
    /// <param name="layout">영역 배치 정의 (section.json 의 slots/repeat/layouts)</param>
    /// <param name="repeat">가로형 칸 수 (없으면 정의의 기본값)</param>
    /// <returns>단계 키 → 줄 목록 (각 줄은 공백으로 구분한 슬롯 키, 빈칸은 ".")</returns>
    public static Dictionary<string, string[]> Areas(JsonObject layout, int? repeat)
    {
        if (layout["repeat"] is JsonObject range)
        {
            var count = RepeatOf(range, repeat);
            var keys = Enumerable.Range(1, count).Select(p => p.ToString()).ToArray();
            return new()
            {
                ["1024"] = [string.Join(' ', keys)],
                ["1023"] = [string.Join(' ', keys)],
                ["779"] = Wrap(keys, Math.Min(count, 2)),
                ["519"] = Wrap(keys, 1)
            };
        }
        var layouts = layout["layouts"] as JsonObject;
        return Breakpoints.ToDictionary(p => p, p => (layouts?[p] as JsonArray)?.Select(x => x?.GetValue<string>() ?? string.Empty).ToArray() ?? []);
    }

    /// <summary>가로형 칸 수를 정의의 범위(min~max) 안으로 맞춥니다 (없으면 기본값).</summary>
    public static int RepeatOf(JsonObject range, int? repeat)
    {
        var min = range["min"]?.GetValue<int>() ?? 2;
        var max = range["max"]?.GetValue<int>() ?? 6;
        return Math.Clamp(repeat ?? range["default"]?.GetValue<int>() ?? min, min, max);
    }

    /// <summary>영역의 슬롯 키 목록 (1024 이상 배치에 나오는 순서, 빈칸 "." 제외)</summary>
    public static List<string> SlotKeys(Dictionary<string, string[]> areas)
        => [.. areas.GetValueOrDefault("1024", []).SelectMany(Tokens).Where(p => p != ".").Distinct()];

    /// <summary>
    /// 슬롯 크기 — 1024 이상 배치에서 그 슬롯이 차지하는 가로(가로 4칸 격자로 환산한 칸 수)와 세로(행 수).
    /// </summary>
    /// <param name="areas">단계별 영역 줄</param>
    /// <param name="slot">슬롯 키</param>
    public static (double Width, int Height) SlotSize(Dictionary<string, string[]> areas, string slot)
    {
        var rows = areas.GetValueOrDefault("1024", []).Select(Tokens).ToList();
        var columns = rows.Select(p => p.Length).DefaultIfEmpty(1).Max();
        var cells = rows.SelectMany((row, r) => row.Select((key, c) => (key, r, c))).Where(p => p.key == slot).ToList();
        if (cells.Count == 0) return (0, 0);
        var span = cells.Max(p => p.c) - cells.Min(p => p.c) + 1;
        return (span * 4.0 / columns, cells.Max(p => p.r) - cells.Min(p => p.r) + 1);
    }

    /// <summary>
    /// 영역 하나의 CSS — 영역 요소(<c>#{id}</c>)의 grid 열 수·영역 줄을 화면 폭 단계별 미디어 쿼리로. 슬롯 요소는 <c>grid-area: s{슬롯 키}</c>.
    /// </summary>
    /// <param name="id">영역 요소 id</param>
    /// <param name="areas">단계별 영역 줄</param>
    public static string Css(string id, Dictionary<string, string[]> areas)
    {
        var css = new StringBuilder();
        foreach (var (breakpoint, query) in new[] { ("1024", ""), ("1023", "(max-width: 1023.98px)"), ("779", "(max-width: 779.98px)"), ("519", "(max-width: 519.98px)") })
        {
            var rows = areas.GetValueOrDefault(breakpoint, []);
            if (rows.Length == 0) continue;
            var columns = rows.Select(p => Tokens(p).Length).Max();
            var template = string.Join(' ', rows.Select(row => $"\"{string.Join(' ', Tokens(row).Select(p => p == "." ? "." : $"s{p}"))}\""));
            var rule = $"#{id}{{grid-template-columns:repeat({columns},minmax(0,1fr));grid-template-areas:{template};}}";
            css.Append(query.Length == 0 ? rule : $"@media {query}{{{rule}}}");
        }
        return css.ToString();
    }

    /// <summary>줄 하나의 슬롯 키들</summary>
    private static string[] Tokens(string row) => row.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>키들을 한 줄에 perRow 개씩 줄로 나눔 (마지막 줄이 모자라면 마지막 칸이 남은 폭을 채움)</summary>
    private static string[] Wrap(string[] keys, int perRow)
        => [.. keys.Chunk(perRow).Select(row => string.Join(' ', row.Concat(Enumerable.Repeat(row[^1], perRow - row.Length))))];
}
