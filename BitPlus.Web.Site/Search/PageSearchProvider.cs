using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Search;
using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Search;

/// <summary>
/// 통합 검색 "페이지" 공급자: 보기 전용(PageView) 규칙의 페이지(/view/{번호})를 찾습니다.
/// 게시 기간 안의 페이지만, 읽기 대상이 지정된 페이지는 대상에 드는 사용자와 관리자에게만 보입니다.
/// </summary>
public class PageSearchProvider : ContentSearchProvider
{
    /// <summary>
    /// <see cref="PageSearchProvider"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    public PageSearchProvider(SiteDbContext db) : base(db) { }

    /// <inheritdoc />
    public override string Name => "page";

    /// <inheritdoc />
    public override int Order => 1;

    /// <inheritdoc />
    public override string TitleOf(HttpContext http) => http.Request.GetLanguage("Pages") ?? "Pages";


    /// <inheritdoc />
    public override async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = query.Http.GetUserAssets();
        var isManager = query.Http.HasPolicy(WebConfig.ManagerPolicyName);
        var pageRules = await this.Db.ContentRules.AsNoTracking().Where(r => r.OnlyView && r.Enabler).Select(r => r.Id).ToListAsync(cancellationToken); // 보기 전용 규칙만 (게시판 글까지 훑지 않게 규칙으로 좁힘)
        var rows = await this.MatchingAsync(query.Keyword, pageRules, q => q.Where(p => p.End == null || p.End >= now), cancellationToken);
        var readable = rows.Where(p => isManager || p.ReadRule.Count == 0 || p.ReadRule.Any(r => ContentPermission.Matches(r.TargetType, r.TargetKey, user))).ToList();
        var titles = await TranslationReader.TitlesAsync(this.Db, query.Http.Request, readable.Select(p => (p.Id, p.Culture))); // 보기 전용 페이지는 항상 다국어: 현재 언어 번역 제목 (없으면 원문)
        var items = readable.Select(p => new SearchItem(titles.TryGetValue(p.Id, out var shown) ? shown.Title : p.Title, $"/view/{p.ContentNo}") { Snippet = p.ContentBody.Snippet(query.Keyword), Date = p.UDate });
        return PageOf(items, query);
    }
}
