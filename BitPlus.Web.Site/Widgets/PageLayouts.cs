using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯 페이지(영역을 배치하는 페이지: home, portal …)의 정의와 배치를 읽고 씁니다.
/// 정의 = <c>Views/Sections/_Pages/{페이지}.json</c> (쓸 수 있는 위젯 카테고리, 기본 배치), 배치 = 설정 <c>PageLayout.{페이지}</c> 의 JSON(없으면 기본 배치).
/// 페이지 키에 <c>@{변형}</c> 을 붙이면(예: <c>side@UserContent</c> — 사이드 연결 메뉴별 위젯) 정의는 앞 페이지 것을 쓰고 배치는 <c>PageLayout.{페이지}@{변형}</c> 에 따로 저장하며, 저장된 적 없으면 앞 페이지의 배치를 씁니다.
/// 배치 JSON: <c>{"sections":[{"id","type","repeat"?,"slots":{"슬롯 키":{"widget","title":{문화권:제목},"settings":{…}}}}]}</c>.
/// </summary>
public static partial class PageLayouts
{
    /// <summary>페이지 정의 폴더 (콘텐츠 루트 기준)</summary>
    public const string PageFolder = "Views/Sections/_Pages";

    /// <summary>배치를 저장하는 설정 키 앞부분 (뒤에 페이지 키)</summary>
    public const string ConfigPrefix = "PageLayout.";

    /// <summary>페이지 키와 변형(메뉴 키 등) 구분 문자</summary>
    public const char VariantSeparator = '@';

    /// <summary>
    /// 변형을 뺀 페이지 키 (예: <c>side@UserContent</c> → <c>side</c>).
    /// </summary>
    /// <param name="page">페이지 키 (변형 포함 가능)</param>
    public static string BaseOf(string page) => page.Split(VariantSeparator, 2)[0];

    /// <summary>변형 키 형식: 영문·숫자·밑줄·하이픈·점 64자 이내 (메뉴 키)</summary>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial System.Text.RegularExpressions.Regex VariantPattern();

    /// <summary>
    /// 페이지 정의를 읽습니다 (키 형식이 아니거나 파일이 없으면 null). 변형이 붙은 키는 앞 페이지의 정의입니다.
    /// </summary>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="page">페이지 키 (변형 포함 가능)</param>
    public static JsonObject? Definition(string contentRoot, string page)
    {
        var parts = page.Split(VariantSeparator, 2);
        if (parts.Length == 2 && !VariantPattern().IsMatch(parts[1])) return null;
        page = parts[0];
        if (!WidgetCatalog.KeyPattern().IsMatch(page)) return null;
        var file = Path.Combine(contentRoot, PageFolder, $"{page}.json");
        if (File.Exists(file) && JsonNode.Parse(File.ReadAllText(file)) is JsonObject json) return json;
        try { return Plugins.PluginManager.Instance.GetPageDefinitionJson(page); }
        catch { return null; }
    }

    /// <summary>페이지가 쓸 수 있는 위젯 카테고리 (<c>*</c> 면 모두)</summary>
    public static string[] Categories(JsonObject definition)
        => [.. (definition["categories"] as JsonArray)?.Select(p => p?.GetValue<string>()).OfType<string>() ?? []];

    /// <summary>사이드 패널 전용 위젯 카테고리 — 이 카테고리 위젯은 페이지 정의에 <c>side</c> 를 직접 적은 페이지(사이드 패널)에만 놓을 수 있음 (<c>*</c> 로는 허용되지 않음)</summary>
    public const string SideCategory = "side";

    /// <summary>
    /// 위젯 종류의 카테고리(쉼표 구분)가 페이지 카테고리에 드는지. 사이드 전용(<see cref="SideCategory"/>) 위젯은 페이지 카테고리에 <c>side</c> 가 있어야 하고,
    /// 그 밖의 위젯은 페이지가 <c>*</c> 이거나 카테고리가 하나라도 겹치면 됩니다.
    /// </summary>
    /// <param name="pageCategories">페이지 카테고리</param>
    /// <param name="widgetCategories">위젯 카테고리 (쉼표 구분)</param>
    public static bool Allows(string[] pageCategories, string widgetCategories)
    {
        var categories = widgetCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (categories.Contains(SideCategory)) return pageCategories.Contains(SideCategory);
        return pageCategories.Contains("*") || categories.Any(pageCategories.Contains);
    }

    /// <summary>
    /// 페이지 배치를 읽습니다: 저장된 설정이 있으면 그것, 없으면(변형이면 앞 페이지의 배치) 페이지 정의의 기본 배치, 둘 다 없으면 빈 배치.
    /// </summary>
    /// <param name="config">사이트 설정</param>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="page">페이지 키</param>
    public static async Task<JsonObject> LoadAsync(WebConfig config, string contentRoot, string page)
    {
        var saved = await config.GetAsync(ConfigPrefix + page);
        if (!string.IsNullOrWhiteSpace(saved) && JsonNode.Parse(saved) is JsonObject layout) return layout;
        if (BaseOf(page) is var basePage && basePage != page) return await LoadAsync(config, contentRoot, basePage);
        return Definition(contentRoot, page)?["layout"]?.DeepClone() as JsonObject ?? new JsonObject { ["sections"] = new JsonArray() };
    }

    /// <summary>
    /// 페이지 배치를 설정에 저장합니다 (설정 캐시도 갱신).
    /// </summary>
    /// <param name="config">사이트 설정</param>
    /// <param name="page">페이지 키</param>
    /// <param name="layout">검사를 마친 배치</param>
    public static Task SaveAsync(WebConfig config, string page, JsonObject layout)
        => config.SetAsync(ConfigPrefix + page, layout.ToJsonString(), DescriptionOf(page));

    /// <summary>
    /// 페이지 배치를 지정 DB 에 저장합니다 (설치 시드처럼 DI 의 DB 가 아직 없을 때).
    /// </summary>
    /// <param name="config">사이트 설정</param>
    /// <param name="db">저장할 DB 컨텍스트</param>
    /// <param name="page">페이지 키</param>
    /// <param name="layout">배치</param>
    public static Task SaveAsync(WebConfig config, BitPlus.Data.IAppDBContext db, string page, JsonObject layout)
        => config.SetAsync(db, ConfigPrefix + page, layout.ToJsonString(), DescriptionOf(page));

    /// <summary>
    /// 변형 페이지(<c>{페이지}@{변형}</c>)의 따로 저장한 배치를 지웁니다 (설정 캐시도 제거) — 이후 앞 페이지의 배치를 씁니다. 저장된 적 없으면 아무것도 하지 않습니다.
    /// </summary>
    /// <param name="config">사이트 설정</param>
    /// <param name="db">사이트 DB 컨텍스트</param>
    /// <param name="page">변형 페이지 키</param>
    public static async Task ResetAsync(WebConfig config, BitPlus.Data.IAppDBContext db, string page)
    {
        var key = ConfigPrefix + page;
        var item = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Settings, p => p.Key == key);
        if (item is not null)
        {
            db.Settings.Remove(item);
            await ((Microsoft.EntityFrameworkCore.DbContext)db).SaveChangesAsync();
        }
        await config.InvalidateAsync(key);
    }

    /// <summary>
    /// 페이지 정의(_Pages/*.json)의 레이아웃 버전이 설정에 저장된 버전보다 높거나 저장된 배치가 없으면 새 레이아웃으로 동기화합니다.
    /// </summary>
    /// <param name="db">사이트 DB 컨텍스트</param>
    /// <param name="config">사이트 설정</param>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="logger">로거</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public static async Task SyncAsync(Data.SiteDbContext db, WebConfig config, string contentRoot, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(contentRoot, PageFolder);
        var pageFiles = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                var page = Path.GetFileNameWithoutExtension(file);
                if (Definition(contentRoot, page) is { } def) pageFiles[page] = def;
            }
        }
        try
        {
            foreach (var (page, def) in Plugins.PluginManager.Instance.GetPluginPageDefinitions())
                if (!pageFiles.ContainsKey(page)) pageFiles[page] = def;
        }
        catch { }

        if (pageFiles.Count == 0) return;

        var defaultBoard = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(db.ContentRules),
            p => p.Type == BitPlus.Data.Content.ContentTypes.Default && !p.OnlyView, cancellationToken);
        var galleryBoard = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(db.ContentRules),
            p => p.Type == BitPlus.Data.Content.ContentTypes.Gallery && !p.OnlyView, cancellationToken);
        var ruleNos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (defaultBoard is not null) ruleNos["free"] = defaultBoard.RuleNo;
        if (galleryBoard is not null) ruleNos["gallery"] = galleryBoard.RuleNo;

        foreach (var (page, def) in pageFiles)
        {
            if (def["layout"]?.DeepClone() is not JsonObject layout) continue;

            var defVersion = def["version"]?.GetValue<int>() ?? layout["version"]?.GetValue<int>() ?? 1;
            var saved = await config.GetAsync(ConfigPrefix + page);
            var savedVersion = 0;
            var isCustomized = false;
            if (!string.IsNullOrWhiteSpace(saved) && JsonNode.Parse(saved) is JsonObject savedObj)
            {
                savedVersion = savedObj["version"]?.GetValue<int>() ?? 0;
                isCustomized = savedObj["customized"]?.GetValue<bool>() ?? false;
            }

            if (isCustomized) continue;

            if (string.IsNullOrWhiteSpace(saved) || savedVersion < defVersion)
            {
                layout["version"] = defVersion;
                Data.SiteSeed.ResolveAliases(layout, ruleNos);
                await SaveAsync(config, db, page, layout);
                await config.InvalidateAsync(ConfigPrefix + page);
                logger?.LogInformation("Synchronized page layout '{Page}' to version {Version}", page, defVersion);
            }
        }
    }

    /// <summary>배치 설정의 설명 문구</summary>
    private static string DescriptionOf(string page) => $"{Properties.Resources.WidgetLayout} ({page})";

    /// <summary>배치의 영역 목록 (없으면 빈 목록)</summary>
    public static IEnumerable<JsonObject> Sections(JsonObject layout) => (layout["sections"] as JsonArray)?.OfType<JsonObject>() ?? [];
}
