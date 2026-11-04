using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Search;
using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Search;

/// <summary>
/// 통합 검색 "게시판" 공급자: 사용 중인 게시판(일반·갤러리·포럼·매뉴얼·FAQ·설문)의 글을 찾습니다.
/// 글마다 게시판 글 보기와 같은 판정(<see cref="BoardAccess.CanOpen"/> — 읽기 권한, 승인, 게시 기간, 비공개 글, 읽기 대상)을 통과한 글만 보이고, 묶음 이름은 게시판 이름입니다.
/// </summary>
public class BoardSearchProvider : ContentSearchProvider
{
    /// <summary>
    /// <see cref="BoardSearchProvider"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    public BoardSearchProvider(SiteDbContext db) : base(db) { }

    /// <inheritdoc />
    public override string Name => "board";

    /// <inheritdoc />
    public override int Order => 2;

    /// <inheritdoc />
    public override string TitleOf(HttpContext http) => http.Request.GetLanguage("ContentBoards") ?? "Boards";


    /// <inheritdoc />
    public override async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = query.Http.GetUserAssets();
        var readable = await BoardAccess.ReadableAsync(this.Db, query.Http, cancellationToken: cancellationToken);
        if (readable.Count == 0) return new SearchResult(0, []);

        var ruleIds = readable.Keys.ToList();
        var translated = readable.Where(p => DefaultContentHandler.Option(p.Value.Rule, TranslationReader.OptionKey)).Select(p => p.Key).ToHashSet(); // 다국어 게시판: 번역 제목도 찾고 표시 제목도 번역으로
        var rows = (await this.MatchingAsync(query.Keyword, ruleIds, null, cancellationToken, translated)).Where(p => BoardAccess.CanOpen(readable[p.ContentRuleId], p, user, now)).ToList();
        var titles = translated.Count == 0 ? [] : await TranslationReader.TitlesAsync(this.Db, query.Http.Request, rows.Skip(query.Skip).Take(query.Size).Where(p => translated.Contains(p.ContentRuleId)).Select(p => (p.Id, p.Culture)));
        var items = rows.Select(p => this.ItemOf(query, readable[p.ContentRuleId].Rule, p, titles.TryGetValue(p.Id, out var shown) ? shown.Title : p.Title));
        return PageOf(items, query);
    }

    /// <summary>결과 항목: 글 보기 주소, 본문 일부, 게시판 이름, 작성일</summary>
    private SearchItem ItemOf(SearchQuery query, ContentRule rule, ContentModel post, string title)
        => new(title, BoardAccess.UrlOf(rule, post)) { Snippet = post.ContentBody.Snippet(query.Keyword), Group = query.Http.Request.DisplayName(rule.Title, $"#{rule.RuleNo}"), Date = post.CDate };
}
