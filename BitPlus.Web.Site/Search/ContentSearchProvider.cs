using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Search;
using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Search;

/// <summary>
/// 컨텐츠(Contents 테이블) 기반 통합 검색 공급자의 공통 부분: 제목·본문에 검색어가 든 공개 글 후보 조회와 메모리 페이지 나누기.
/// 공급자마다 대상 규칙과 읽기 권한 판정만 다르게 구현합니다.
/// ponytail: 제목·본문 LIKE 검색(본문 HTML 태그 안 글자도 걸림, 게시판 본문은 최근 글만)과 최신 후보 <see cref="MaxCandidates"/> 건 안에서 권한을 거름 — 글이 많아지면 전문 검색 인덱스·DB 쪽 권한 조건으로 옮길 것.
/// </summary>
public abstract class ContentSearchProvider : ISearchProvider
{
    /// <summary>권한을 거르기 전에 가져오는 최신 후보 수 (전체 건수도 이 안에서 셈)</summary>
    protected const int MaxCandidates = 500;

    /// <summary>사이트 DB</summary>
    protected SiteDbContext Db { get; }

    /// <summary>
    /// <see cref="ContentSearchProvider"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    protected ContentSearchProvider(SiteDbContext db)
    {
        this.Db = db;
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public abstract string TitleOf(HttpContext http);


    /// <inheritdoc />
    public abstract Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 표시·게시 상태이고 게시 시작 시각이 지난 글 중 제목(전체)·본문(대상 규칙의 최근 글, <see cref="ContentSearchWindow"/>)에 검색어가 든 글 (규칙·읽기 대상 포함, 최신순).
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="ruleIds">대상 규칙 (규칙마다 따로 찾아 합침 — 본문은 그 규칙의 최근 글만)</param>
    /// <param name="refine">대상 글 조건을 더 좁히는 식 (없으면 그대로)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <param name="translated">번역 제목도 찾을 규칙 (다국어 게시판, 없으면 원문 제목만)</param>
    /// <returns>최신 후보 <see cref="MaxCandidates"/> 건 (읽기 규칙·속성 포함)</returns>
    protected async Task<List<ContentModel>> MatchingAsync(string keyword, List<Guid> ruleIds, Func<IQueryable<ContentModel>, IQueryable<ContentModel>>? refine, CancellationToken cancellationToken, ISet<Guid>? translated = null)
    {
        var now = DateTime.UtcNow;
        IQueryable<ContentModel> Public() => (refine ?? (q => q))(this.Db.Contents.AsNoTracking().Where(p => p.ContentNo != null && p.Visible && p.Published && (p.Start == null || p.Start <= now)));
        var ids = new List<(Guid Id, DateTime CDate)>();
        // 게시판마다 따로 찾는다 — 여러 게시판을 IN 으로 한 번에 찾으면 게시판·작성일 인덱스를 못 탐
        foreach (var ruleId in ruleIds)
        {
            var bodyFrom = await ContentSearchWindow.BodyFromAsync(this.Db.Contents.Where(p => p.ContentRuleId == ruleId), cancellationToken);
            var withTranslation = translated?.Contains(ruleId) == true;
            var matched = ContentSearchWindow.WhereMatches(Public().Where(p => p.ContentRuleId == ruleId), p => p.Title.Contains(keyword) || (withTranslation && p.Translations.Any(t => t.Title.Contains(keyword))), keyword, bodyFrom);
            ids.AddRange((await matched.OrderByDescending(p => p.CDate).Take(MaxCandidates).Select(p => new { p.Id, p.CDate }).ToListAsync(cancellationToken)).Select(p => (p.Id, p.CDate)));
        }
        var top = ids.OrderByDescending(p => p.CDate).Take(MaxCandidates).Select(p => p.Id).ToList();
        var rows = await this.Db.Contents.AsNoTracking().Include(p => p.ReadRule).Include(p => p.Properties).Where(p => top.Contains(p.Id)).ToListAsync(cancellationToken);
        return [.. rows.OrderByDescending(p => p.CDate)];
    }

    /// <summary>
    /// 권한을 거친 항목 전체에서 요청한 페이지를 잘라 결과로 만듭니다.
    /// </summary>
    /// <param name="items">볼 수 있는 항목 (최신순)</param>
    /// <param name="query">검색 요청</param>
    protected static SearchResult PageOf(IEnumerable<SearchItem> items, SearchQuery query)
    {
        var list = items.ToList();
        return new SearchResult(list.Count, [.. list.Skip(query.Skip).Take(query.Size)]);
    }
}
