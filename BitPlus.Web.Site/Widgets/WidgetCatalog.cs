using BitPlus.Web.Site.Data;
using BitPlus.Web.Site.Data.Widget;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯·영역 폴더(<c>Views/Widgets/{키}/widget.json</c>, <c>Views/Sections/{키}/section.json</c>)를 정보 테이블과 맞춥니다 (설치 때와 사이트 시작 때).
/// 없으면 추가, JSON 의 version 이 더 크면 정보 갱신(사용 여부는 유지), 폴더에서 사라지면 Available = false, 읽을 수 없는 JSON 은 그 폴더만 건너뛰고 로그를 남깁니다.
/// </summary>
public static partial class WidgetCatalog
{
    /// <summary>위젯 폴더 (콘텐츠 루트 기준)</summary>
    public const string WidgetFolder = "Views/Widgets";

    /// <summary>영역 폴더 (콘텐츠 루트 기준)</summary>
    public const string SectionFolder = "Views/Sections";

    /// <summary>키 형식 — 영문·숫자 (폴더 이름 = 키, 뷰 경로에 들어가므로 이 형식만 받음)</summary>
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]{0,63}$")]
    public static partial Regex KeyPattern();

    /// <summary>위젯 출력 방식 캐시 (키 → 한 페이지에 하나만, 공통 카드 틀 사용) — widget.json 은 사이트 실행 중 바뀌지 않으므로 처음 읽은 값을 씀</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (bool Single, bool Frame)> flags = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// widget.json 의 출력 방식 값을 읽습니다 (정보 테이블에 칸이 없는 값이라 파일에서 직접, 한 번 읽으면 캐시).
    /// <c>single</c>: true 면 한 페이지(배치)에 하나만 놓을 수 있음 (기본 false), <c>frame</c>: false 면 공통 카드 틀(제목 줄) 없이 위젯 출력 뷰만 그림 (기본 true).
    /// 키 형식이 아니거나 파일이 없으면 기본값입니다.
    /// </summary>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="key">위젯 키</param>
    public static (bool Single, bool Frame) Flags(string contentRoot, string? key)
    {
        if (key is null || !KeyPattern().IsMatch(key)) return (false, true);
        return flags.GetOrAdd(key, k =>
        {
            try
            {
                var file = Path.Combine(contentRoot, WidgetFolder, k, "widget.json");
                if (!File.Exists(file) || JsonNode.Parse(File.ReadAllText(file)) is not JsonObject json) return (false, true);
                return (json["single"] is JsonValue s && s.TryGetValue<bool>(out var single) && single, json["frame"] is not JsonValue f || !f.TryGetValue<bool>(out var frame) || frame);
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException)
            {
                return (false, true);
            }
        });
    }

    /// <summary>
    /// 위젯·영역 정보 테이블을 폴더 JSON 과 맞추고 저장합니다.
    /// </summary>
    /// <param name="db">사이트 DB</param>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="logger">잘못된 JSON 을 남길 로거 (없으면 남기지 않음)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public static async Task SyncAsync(SiteDbContext db, string contentRoot, ILogger? logger, CancellationToken cancellationToken = default)
    {
        var widgets = ReadFolder(Path.Combine(contentRoot, WidgetFolder), "widget.json", logger);
        var sections = ReadFolder(Path.Combine(contentRoot, SectionFolder), "section.json", logger);
        var widgetRows = await db.WidgetTypes.ToDictionaryAsync(p => p.Key, cancellationToken);
        var sectionRows = await db.SectionTypes.ToDictionaryAsync(p => p.Key, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var (key, json) in widgets)
        {
            var row = widgetRows.GetValueOrDefault(key);
            if (row is null) db.WidgetTypes.Add(row = new WidgetType { Key = key, Version = -1 });
            row.Available = true;
            if (row.Version >= Version(json)) continue;
            var size = json["size"] as JsonObject;
            (row.Version, row.Name, row.Description, row.Settings, row.UDate) = (Version(json), Text(json["name"]) ?? "{}", Text(json["description"]), Text(json["settings"]) ?? "[]", now);
            row.Categories = string.Join(',', (json["categories"] as JsonArray)?.Select(p => p?.GetValue<string>()).OfType<string>() ?? []);
            (row.MinWidth, row.MinHeight) = Size(size?["min"], 1, 1);
            (row.MaxWidth, row.MaxHeight) = Size(size?["max"], 4, 4);
        }
        foreach (var row in widgetRows.Values.Where(p => !widgets.ContainsKey(p.Key))) row.Available = false;

        foreach (var (key, json) in sections)
        {
            var row = sectionRows.GetValueOrDefault(key);
            if (row is null) db.SectionTypes.Add(row = new SectionType { Key = key, Version = -1 });
            row.Available = true;
            if (row.Version >= Version(json)) continue;
            var layout = new JsonObject { ["slots"] = json["slots"]?.DeepClone(), ["repeat"] = json["repeat"]?.DeepClone(), ["layouts"] = json["layouts"]?.DeepClone() };
            (row.Version, row.Name, row.Description, row.Layout, row.SortOrder, row.UDate) = (Version(json), Text(json["name"]) ?? "{}", Text(json["description"]), layout.ToJsonString(), json["order"]?.GetValue<int>() ?? 0, now);
        }
        foreach (var row in sectionRows.Values.Where(p => !sections.ContainsKey(p.Key))) row.Available = false;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>폴더 아래 하위 폴더마다 JSON 파일을 읽어 (키 = 폴더 이름) 돌려줍니다. 키 형식이 아니거나 읽을 수 없으면 건너뜁니다.</summary>
    private static Dictionary<string, JsonObject> ReadFolder(string folder, string fileName, ILogger? logger)
    {
        var result = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folder)) return result;
        foreach (var dir in Directory.GetDirectories(folder))
        {
            var key = Path.GetFileName(dir);
            var file = Path.Combine(dir, fileName);
            if (!KeyPattern().IsMatch(key) || !File.Exists(file)) continue;
            try
            {
                if (JsonNode.Parse(File.ReadAllText(file)) is JsonObject json) result[key] = json;
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException)
            {
                logger?.LogWarning("Widget catalog skipped [{File}]: {Message}", file, ex.Message);
            }
        }
        return result;
    }

    /// <summary>JSON 의 version (없으면 0)</summary>
    private static int Version(JsonObject json) => json["version"]?.GetValue<int>() ?? 0;

    /// <summary>JSON 노드를 문자열로 (없으면 null)</summary>
    private static string? Text(JsonNode? node) => node?.ToJsonString();

    /// <summary>[가로, 세로] 배열을 크기로 (없으면 기본값)</summary>
    private static (double, int) Size(JsonNode? node, double width, int height)
        => node is JsonArray pair && pair.Count == 2 ? (pair[0]!.GetValue<double>(), pair[1]!.GetValue<int>()) : (width, height);
}
