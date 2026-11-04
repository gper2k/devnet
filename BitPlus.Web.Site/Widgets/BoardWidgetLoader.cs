using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 게시판 위젯 로더의 공통 부분: 설정의 게시판(ruleNos, 비우면 전체) 중 현재 사용자가 읽을 수 있는 게시판의 원본 글을 최신순으로,
/// 글 보기와 같은 판정(<see cref="BoardAccess.CanOpen"/>)을 통과한 것만 count 개 모아 <see cref="WidgetPost"/> 목록으로 돌려줍니다.
/// 하위 클래스는 대상 형식과 추가 조건(공지만 등)만 정합니다.
/// ponytail: 최신 후보 count × 4 건 안에서 권한을 거름 — 잠긴 글이 많으면 count 보다 적게 나올 수 있음, 필요하면 더 가져오기를 반복할 것.
/// </summary>
public abstract class BoardWidgetLoader : IWidgetLoader
{
    /// <summary>사이트 DB</summary>
    protected SiteDbContext Db { get; }

    /// <summary>
    /// <see cref="BoardWidgetLoader"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    protected BoardWidgetLoader(SiteDbContext db)
    {
        this.Db = db;
    }

    /// <inheritdoc />
    public abstract string Widget { get; }

    /// <summary>대상 게시판 형식 (null 이면 모든 게시판 형식)</summary>
    protected virtual ContentTypes[]? Types => null;

    /// <summary>글 조건을 더 좁히는 틈새 (기본: 그대로)</summary>
    /// <param name="query">글 조회식</param>
    protected virtual IQueryable<ContentModel> Refine(IQueryable<ContentModel> query) => query;

    /// <summary>대표 이미지가 필요한지 (기본: 설정 image, 갤러리는 항상)</summary>
    /// <param name="context">위젯 정보</param>
    protected virtual bool NeedImage(WidgetContext context) => context.Bool("image", false);

    /// <inheritdoc />
    public virtual async Task<object?> LoadAsync(WidgetContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var count = Math.Clamp(context.Int("count", 5), 1, 50);
        var user = context.Http.GetUserAssets();
        var boards = await BoardAccess.ReadableAsync(this.Db, context.Http, context.Ints("ruleNos"), this.Types, cancellationToken);
        if (boards.Count == 0) return new List<WidgetPost>();
        if (boards.Count == 1) context.MoreUrl = $"/content/{boards.Values.First().Rule.RuleNo}"; // 대상 게시판이 하나(하나만 골랐거나, 비웠는데 읽을 수 있는 그 형식 게시판이 하나)면 +more = 그 목록

        // 후보 식별키만 게시판마다 (게시판·작성일 인덱스로) 먼저 고른 뒤 합쳐 최신순으로 자르고, 그 글만 본문·읽기 규칙·속성과 함께 읽는다
        // (게시판 여러 개를 IN 으로 한 번에 고르면 최신순 인덱스를 못 타고 글 전체를 훑음)
        var candidates = new List<(Guid Id, DateTime CDate)>();
        foreach (var ruleId in boards.Keys)
        {
            var query = this.Db.Contents.AsNoTracking()
                .Where(p => p.ContentRuleId == ruleId && p.ContentNo != null && (p.LevelNo == null || p.LevelNo == 0) && p.Visible && p.Published && (p.Start == null || p.Start <= now));
            candidates.AddRange((await this.Refine(query).OrderByDescending(p => p.CDate).Take(count * 4).Select(p => new { p.Id, p.CDate }).ToListAsync(cancellationToken)).Select(p => (p.Id, p.CDate)));
        }
        var ids = candidates.OrderByDescending(p => p.CDate).Take(count * 4).Select(p => p.Id).ToList();
        var rows = (await this.Db.Contents.AsNoTracking().Include(p => p.ReadRule).Include(p => p.Properties).Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken))
            .OrderByDescending(p => p.CDate).Where(p => BoardAccess.CanOpen(boards[p.ContentRuleId], p, user, now)).Take(count).ToList();
        var needImage = this.NeedImage(context);
        var thumbs = needImage ? await this.ThumbsAsync([.. rows.Select(p => p.Id)], cancellationToken) : [];
        var shown = await this.TranslationsAsync(context, rows.Where(p => DefaultContentHandler.SupportsTranslation(boards[p.ContentRuleId].Rule)), cancellationToken);
        return rows.Select(p =>
        {
            var (title, body) = shown.TryGetValue(p.Id, out var translated) ? translated : (p.Title, p.ContentBody);
            return new WidgetPost
            {
                Title = title,
                Url = BoardAccess.UrlOf(boards[p.ContentRuleId].Rule, p),
                Board = context.Http.Request.DisplayName(boards[p.ContentRuleId].Rule.Title, $"#{boards[p.ContentRuleId].Rule.RuleNo}"),
                Date = p.CDate,
                Summary = boards[p.ContentRuleId].Rule.Type is ContentTypes.Survey ? null : body.Snippet(null, 120), // 설문 본문은 JSON 이라 요약하지 않음
                Image = needImage ? (thumbs.TryGetValue(p.Id, out var file) ? $"/system/thumb/{file}" : DefaultContentHandler.FirstImageOf(body) ?? DefaultContentHandler.FirstImageOf(p.ContentBody)) : null
            };
        }).ToList();
    }

    /// <summary>
    /// 다국어 게시판 글의 현재 언어 번역 제목·본문 (글 목록과 같은 규칙: 원문 언어가 현재 언어와 다른 글만, 번역이 없으면 빠져 원문을 씀). 한 번에 조회합니다.
    /// </summary>
    /// <param name="context">위젯 정보</param>
    /// <param name="rows">번역을 쓸 수 있는 게시판의 글</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task<Dictionary<Guid, (string Title, string Body)>> TranslationsAsync(WidgetContext context, IEnumerable<ContentModel> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Where(p => TranslationReader.NeedsTranslation(context.Http.Request, p.Culture)).Select(p => p.Id).ToList();
        if (ids.Count == 0) return [];
        var culture = TranslationReader.Current;
        return await this.Db.ContentTranslations.AsNoTracking().Where(p => ids.Contains(p.ContentId) && p.Culture == culture).ToDictionaryAsync(p => p.ContentId, p => (p.Title, p.Body), cancellationToken);
    }

    /// <summary>글마다 첫 첨부 이미지(올린 순서)의 파일 식별키 — 갤러리 목록과 같은 대표 이미지</summary>
    private async Task<Dictionary<Guid, Guid>> ThumbsAsync(List<Guid> ids, CancellationToken cancellationToken)
        => (await this.Db.ContentFiles.AsNoTracking().Where(m => ids.Contains(m.ContentId) && m.Registed && m.FileInfo!.FileMime.StartsWith("image/"))
            .OrderBy(m => m.CDate).Select(m => new { m.ContentId, m.FileInfoId }).ToListAsync(cancellationToken))
            .GroupBy(m => m.ContentId).ToDictionary(g => g.Key, g => g.First().FileInfoId);
}
