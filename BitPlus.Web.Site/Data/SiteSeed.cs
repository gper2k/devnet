using BitPlus.Data.Account;
using BitPlus.Data.CommonModel;
using BitPlus.Data.Content;
using BitPlus.Data.File;
using BitPlus.Data.SiteMap;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Site.Widgets;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// 사이트 기본 구성 시드 (설치 때 한 번): <c>Data/SiteSeed.json</c> 의 기본 게시판(설정·첨부 규칙·접근 권한·게시판 메뉴)과 샘플 글, 사용자 메뉴, 그리고 위젯 페이지 기본 배치의 게시판 별칭 해석.
/// 게시판은 파일의 별칭(<c>alias</c>)으로 서로·메뉴·배치에서 가리키고, 설치 때 정해진 게시판 번호로 바꿉니다: 메뉴 위치 <c>"@별칭"</c>, 메뉴 주소 <c>{별칭}</c>, 배치의 ruleNos/ruleNo <c>"@별칭"</c>.
/// 보기 전용이 아닌 게시판이 이미 있으면(설치 재시도 등) 아무것도 하지 않습니다.
/// </summary>
public static class SiteSeed
{
    /// <summary>시드 파일 경로 (콘텐츠 루트 기준)</summary>
    public const string FilePath = "Data/SiteSeed.json";

    /// <summary>
    /// 시드 파일로 기본 게시판·샘플 글·사용자 메뉴를 만들고, 위젯 페이지 기본 배치의 게시판 별칭을 번호로 바꿔 배치 설정으로 저장합니다. 단계마다 저장합니다(게시판 메뉴가 저장된 상위 메뉴를 찾으므로).
    /// </summary>
    /// <param name="db">설치 DB 컨텍스트 (역할·시스템 메뉴가 저장된 뒤)</param>
    /// <param name="config">사이트 설정</param>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="roles">역할 이름 → 식별키 (시드의 접근 대상)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public static async Task ApplyAsync(SiteDbContext db, WebConfig config, string contentRoot, IReadOnlyDictionary<string, Guid> roles, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(contentRoot, FilePath);
        if (!File.Exists(file) || await db.ContentRules.AnyAsync(p => !p.OnlyView, cancellationToken)) return;
        if (JsonNode.Parse(await File.ReadAllTextAsync(file, cancellationToken)) is not JsonObject seed) return;

        var ruleNos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var board in (seed["boards"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var rule = await AddBoardAsync(db, board, roles, ruleNos, cancellationToken);
            ruleNos[board["alias"]!.GetValue<string>()] = rule.RuleNo;
        }
        await AddMenusAsync(db, seed["menus"] as JsonArray, ruleNos, cancellationToken);
        await SaveLayoutsAsync(db, config, contentRoot, ruleNos, cancellationToken);
    }

    /// <summary>
    /// 컨텐츠 규칙 하나를 만듭니다 (규칙 번호 = 저장된 최대 번호와 추가 중인 번호 중 큰 값 + 1). 호출하는 쪽이 추가·저장합니다.
    /// </summary>
    /// <param name="db">DB 컨텍스트</param>
    /// <param name="titles">문화권 → 제목</param>
    /// <param name="onlyView">보기 전용 여부</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public static async Task<ContentRule> NewRuleAsync(SiteDbContext db, IEnumerable<KeyValuePair<string, string>> titles, bool onlyView, CancellationToken cancellationToken = default)
    {
        var saved = await db.ContentRules.MaxAsync(p => (int?)p.RuleNo, cancellationToken) ?? 0;
        var pending = db.ContentRules.Local.Select(p => p.RuleNo).DefaultIfEmpty(0).Max();
        var rule = new ContentRule { OnlyView = onlyView, RuleNo = Math.Max(saved, pending) + 1 };
        foreach (var (culture, title) in titles)
            rule.Title.Add(new DpName<ContentRule, Guid>(rule.Id, culture, title));
        return rule;
    }

    /// <summary>게시판 하나(설정·첨부 규칙·접근 권한·메뉴·글)를 만들고 저장합니다.</summary>
    private static async Task<ContentRule> AddBoardAsync(SiteDbContext db, JsonObject board, IReadOnlyDictionary<string, Guid> roles, Dictionary<string, int> ruleNos, CancellationToken cancellationToken)
    {
        var rule = await NewRuleAsync(db, TextsOf(board["title"]), false, cancellationToken);
        rule.Type = Enum.Parse<ContentTypes>(board["type"]!.GetValue<string>(), true);
        rule.Category = board["category"]?.GetValue<string>();
        if (board["options"] is JsonObject options)
            ContentManager.SetRuleProperty(rule, ContentOptions.PropertyKey, options.ToJsonString());
        foreach (var (mode, role) in AccessOf(board["access"], roles))
            rule.AccessRule.Add(new ContentAccessRule { ContentRuleId = rule.Id, AccessMode = Enum.Parse<ContentAccessMode>(mode, true), TargetType = AccountTypes.Role, TargetKey = role });
        if (board["file"] is JsonObject file) rule.FileRuleId = AddFileRule(db, rule.RuleNo, file, roles).Id;
        db.ContentRules.Add(rule);
        AddPosts(db, rule, board["posts"] as JsonArray);
        await db.SaveChangesAsync(cancellationToken);

        var menu = board["menu"] as JsonObject;
        var parent = menu?["parent"]?.GetValue<string>();
        if (parent?.StartsWith('@') == true) parent = ruleNos.TryGetValue(parent[1..], out var no) ? DefaultContentHandler.BoardMenuKey(no) : null;
        await DefaultContentHandler.BoardMenuApplyAsync(db, rule, parent, menu?["icon"]?.GetValue<string>(), menu?["hidden"]?.GetValue<bool>() ?? false);
        if (menu?["sort"]?.GetValue<int>() is int sort && db.Menus.Local.FirstOrDefault(p => p.Key == DefaultContentHandler.BoardMenuKey(rule.RuleNo)) is { } added) added.SortOrder = sort;
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    /// <summary>게시판 전용 첨부 파일 규칙(<c>Content_{번호}</c>, 경로 <c>Content/{번호}</c>)을 추가합니다.</summary>
    private static UpFileRule AddFileRule(SiteDbContext db, int ruleNo, JsonObject file, IReadOnlyDictionary<string, Guid> roles)
    {
        var fileRule = new UpFileRule
        {
            Name = DefaultContentHandler.FileRuleNameOf(ruleNo),
            RegistWebRoot = $"Content/{ruleNo}",
            AllowExtensions = file["extensions"]?.GetValue<string>(),
            AllowCount = file["count"]?.GetValue<int>() ?? -1,
            AllowSize = file["size"]?.GetValue<long>() ?? -1,
            AllowTotalSize = file["totalSize"]?.GetValue<long>() ?? -1
        };
        foreach (var (mode, role) in AccessOf(file["access"], roles))
            fileRule.AccessRules.Add(new UpFileAccessRule { RuleId = fileRule.Id, Mode = Enum.Parse<UpFileMode>(mode, true), TargetType = AccountTypes.Role, TargetKey = role });
        db.FileRules.Add(fileRule);
        return fileRule;
    }

    /// <summary>
    /// 샘플 글을 추가합니다: 글 번호는 1부터 차례로, 원글 아래 children(포럼 답글·설명서 하위 문서)은 원글 번호를 대상 번호로, 단계 1, 정렬 순서 1부터. 작성일 = 지금 - daysAgo (UTC), 작성자 없음.
    /// </summary>
    private static void AddPosts(SiteDbContext db, ContentRule rule, JsonArray? posts)
    {
        var no = 0;
        var now = DateTime.UtcNow;
        ContentModel Add(JsonObject post, int target, int level, int sort)
        {
            var date = now.AddDays(-(post["daysAgo"]?.GetValue<double>() ?? 0));
            var model = new ContentModel
            {
                ContentRuleId = rule.Id,
                Title = post["title"]!.GetValue<string>(),
                Category = post["category"]?.GetValue<string>(),
                ContentBody = post["body"]?.GetValue<string>() ?? string.Empty,
                ContentNo = ++no,
                TargetNo = target == 0 ? no : target,
                LevelNo = level,
                SortNo = sort,
                IsNotice = post["notice"]?.GetValue<bool>() ?? false,
                Approved = true,
                Visible = true,
                Published = true,
                CDate = date,
                UDate = date
            };
            db.Contents.Add(model);
            return model;
        }

        foreach (var post in (posts ?? []).OfType<JsonObject>())
        {
            var parent = Add(post, 0, 0, 0);
            var sort = 0;
            foreach (var child in (post["children"] as JsonArray ?? []).OfType<JsonObject>())
                Add(child, parent.ContentNo!.Value, 1, ++sort);
        }
    }

    /// <summary>사용자 메뉴(예: User &gt; Content 아래 게시판 바로가기)를 추가합니다. 주소의 <c>{별칭}</c> 은 게시판 번호로 바꾸고, 이미 있는 키·없는 상위 메뉴는 건너뜁니다.</summary>
    private static async Task AddMenusAsync(SiteDbContext db, JsonArray? menus, Dictionary<string, int> ruleNos, CancellationToken cancellationToken)
    {
        foreach (var item in (menus ?? []).OfType<JsonObject>())
        {
            var key = item["key"]!.GetValue<string>();
            var parent = await db.Menus.FirstOrDefaultAsync(p => p.Key == item["parent"]!.GetValue<string>(), cancellationToken);
            if (parent is null || await db.Menus.AnyAsync(p => p.Key == key, cancellationToken)) continue;
            var action = ruleNos.Aggregate(item["action"]?.GetValue<string>() ?? string.Empty, (text, p) => text.Replace($"{{{p.Key}}}", p.Value.ToString()));
            var menu = new AppMenu(key, LinkType.Move, action, null, parent, item["sort"]?.GetValue<int>() ?? 0);
            foreach (var (culture, name) in TextsOf(item["names"]))
                menu.DpNames.Add(new DpName<AppMenu, string>(key, culture, name));
            db.Menus.Add(menu);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>위젯 페이지 정의마다 기본 배치에 게시판 별칭(<c>"@별칭"</c>)이 있으면 번호로 바꿔 배치 설정으로 저장합니다 (별칭이 없으면 정의의 기본 배치를 그대로 씀).</summary>
    private static async Task SaveLayoutsAsync(SiteDbContext db, WebConfig config, string contentRoot, Dictionary<string, int> ruleNos, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(contentRoot, PageLayouts.PageFolder);
        if (!Directory.Exists(folder)) return;
        foreach (var page in Directory.GetFiles(folder, "*.json").Select(Path.GetFileNameWithoutExtension))
        {
            if (PageLayouts.Definition(contentRoot, page!)?["layout"]?.DeepClone() is not JsonObject layout) continue;
            if (ResolveAliases(layout, ruleNos)) await PageLayouts.SaveAsync(config, db, page!, layout);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>JSON 안의 <c>"@별칭"</c> 문자열 값을 게시판 번호로 바꿉니다 (모르는 별칭은 그대로 — 저장 때 설정 정리가 버림). 바꾼 것이 있으면 true.</summary>
    internal static bool ResolveAliases(JsonNode? node, Dictionary<string, int> ruleNos)
    {
        var changed = false;
        if (node is JsonObject obj)
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (AliasOf(obj[key], ruleNos) is int no) { obj[key] = no; changed = true; }
                else changed |= ResolveAliases(obj[key], ruleNos);
            }
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++)
            {
                if (AliasOf(array[i], ruleNos) is int no) { array[i] = no; changed = true; }
                else changed |= ResolveAliases(array[i], ruleNos);
            }
        return changed;
    }

    /// <summary>값이 알려진 게시판 별칭 문자열(<c>"@별칭"</c>)이면 그 번호, 아니면 null.</summary>
    private static int? AliasOf(JsonNode? node, Dictionary<string, int> ruleNos)
        => node is JsonValue value && value.GetValueKind() == JsonValueKind.String && value.GetValue<string>() is ['@', .. var alias] && ruleNos.TryGetValue(alias, out var no) ? no : null;

    /// <summary>{문화권: 글자} 객체를 쌍 목록으로 (빈 값 제외).</summary>
    private static IEnumerable<KeyValuePair<string, string>> TextsOf(JsonNode? node)
        => (node as JsonObject ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Value?.GetValue<string>())).Select(p => KeyValuePair.Create(p.Key, p.Value!.GetValue<string>()));

    /// <summary>[{mode, role}] 목록을 (모드 이름, 역할 식별키) 쌍으로 (없는 역할 제외).</summary>
    private static IEnumerable<(string Mode, Guid Role)> AccessOf(JsonNode? node, IReadOnlyDictionary<string, Guid> roles)
        => (node as JsonArray ?? []).OfType<JsonObject>()
            .Select(p => (Mode: p["mode"]?.GetValue<string>(), Role: p["role"]?.GetValue<string>()))
            .Where(p => p.Mode is not null && p.Role is not null && roles.ContainsKey(p.Role))
            .Select(p => (p.Mode!, roles[p.Role!]));

    /// <summary>
    /// 기본 게시판(자유게시판, 갤러리)의 구버전 샘플 글을 최신 CMS 소개 글(SiteSeed.json)로 교체합니다.
    /// </summary>
    public static async Task SyncSamplePostsAsync(SiteDbContext db, string contentRoot, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(contentRoot, FilePath);
        if (!File.Exists(file)) return;
        if (JsonNode.Parse(await File.ReadAllTextAsync(file, cancellationToken)) is not JsonObject seed) return;

        var defaultBoard = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.ContentRules, p => p.Type == ContentTypes.Default && !p.OnlyView, cancellationToken);
        var galleryBoard = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.ContentRules, p => p.Type == ContentTypes.Gallery && !p.OnlyView, cancellationToken);

        var boards = seed["boards"] as JsonArray;
        if (boards is null) return;

        var oldSampleTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "자유 게시판 이용 안내", "비 오는 밤, 네온 거리의 스포츠카", "여름 들판에서 보내는 느린 오후", "바다 밑에 잠든 오래된 자동차",
            "푸른 빙하", "꿈꾸는 고래", "꽃밭의 강아지", "모자 쓴 토끼", "숲속 드러머", "아기 용과 수정 구슬", "유리 상자 속 풍경", "노을 지는 여름 들판"
        };

        foreach (var board in boards.OfType<JsonObject>())
        {
            var alias = board["alias"]?.GetValue<string>();
            var targetRule = alias switch
            {
                "free" => defaultBoard,
                "gallery" => galleryBoard,
                _ => null
            };
            if (targetRule is null) continue;

            var existingOldPosts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.Contents.Where(p => p.ContentRuleId == targetRule.Id && oldSampleTitles.Contains(p.Title)), cancellationToken);

            var hasAnyPosts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                db.Contents, p => p.ContentRuleId == targetRule.Id, cancellationToken);

            if (existingOldPosts.Count > 0 || !hasAnyPosts)
            {
                if (existingOldPosts.Count > 0)
                {
                    db.Contents.RemoveRange(existingOldPosts);
                    await db.SaveChangesAsync(cancellationToken);
                }

                AddPosts(db, targetRule, board["posts"] as JsonArray);
                await db.SaveChangesAsync(cancellationToken);
                logger?.LogInformation("Synchronized seed sample posts for board '{Alias}' ({Count} posts updated)", alias, existingOldPosts.Count);
            }
        }
    }
}
