using BitPlus.Data.Account.Model.Cache;
using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// 게시판 글을 사이트 여러 곳(통합 검색, 위젯)에서 보여 줄 때의 공통 읽기 판정: 현재 사용자가 읽을 수 있는 게시판(규칙 + 권한)과
/// 글마다 글 보기와 같은 판정(<see cref="PostAccessRule.Check"/>), 글 보기 주소.
/// </summary>
public static class BoardAccess
{
    /// <summary>게시판으로 다루는 규칙 형식 (보기 전용 제외)</summary>
    public static readonly ContentTypes[] BoardTypes = [ContentTypes.Default, ContentTypes.Gallery, ContentTypes.Forum, ContentTypes.Blog, ContentTypes.Manual, ContentTypes.FAQ, ContentTypes.Survey];

    /// <summary>한 요청 안에서 읽을 수 있는 전체 게시판 목록을 재사용하는 HttpContext.Items 키</summary>
    private const string ReadableCacheKey = "BoardAccess.Readable";

    /// <summary>
    /// 현재 사용자가 읽을 수 있는 사용 중인 게시판을 규칙 식별키별로 돌려줍니다 (규칙 이름·접근 권한·승인자·속성 포함).
    /// </summary>
    /// <param name="db">사이트 DB</param>
    /// <param name="http">현재 요청</param>
    /// <param name="ruleNos">이 규칙 번호만 (비우면 전체)</param>
    /// <param name="types">이 형식만 (비우면 <see cref="BoardTypes"/>)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public static async Task<Dictionary<Guid, (ContentRule Rule, ContentPermission Permission)>> ReadableAsync(SiteDbContext db, HttpContext http, ICollection<int>? ruleNos = null, ContentTypes[]? types = null, CancellationToken cancellationToken = default)
    {
        if (http.Items[ReadableCacheKey] is not List<(ContentRule Rule, ContentPermission Permission)> all)
        {
            var user = http.GetUserAssets();
            var isPolicyAdmin = http.HasPolicy(WebConfig.ManagerPolicyName) || http.HasPolicy(WebConfig.ContentManagerPolicyName);
            var rules = await db.ContentRules.AsNoTracking().Include(p => p.Title).Include(p => p.AccessRule).Include(p => p.Approvers).Include(p => p.Properties)
                .Where(p => p.Enabler && !p.OnlyView && BoardTypes.Contains(p.Type)).ToListAsync(cancellationToken);
            all = [.. rules.Select(rule => (rule, permission: ContentPermission.Resolve(rule, user, isPolicyAdmin))).Where(p => p.permission.CanRead || p.permission.IsAdmin)];
            http.Items[ReadableCacheKey] = all;
        }
        var allowed = types ?? BoardTypes;
        return all.Where(p => allowed.Contains(p.Rule.Type) && (ruleNos is not { Count: > 0 } || ruleNos.Contains(p.Rule.RuleNo)))
            .ToDictionary(p => p.Rule.Id, p => (p.Rule, p.Permission));
    }

    /// <summary>
    /// 글을 현재 사용자가 열 수 있는지 (글 보기와 같은 판정). 글은 읽기 대상(ReadRule)·속성을 불러와 있어야 합니다.
    /// </summary>
    /// <param name="board">글의 게시판 (규칙 + 권한)</param>
    /// <param name="post">글</param>
    /// <param name="user">현재 사용자 자산</param>
    /// <param name="now">기준 시각 (UTC)</param>
    public static bool CanOpen((ContentRule Rule, ContentPermission Permission) board, ContentModel post, UserAssets? user, DateTime now)
        => PostAccessRule.Check(board.Rule, board.Permission, post, user, false, now) == PostAccessRule.Allowed;

    /// <summary>글 보기 주소: FAQ 는 목록, 포럼 댓글은 토픽 글, 그 밖에는 글 보기</summary>
    public static string UrlOf(ContentRule rule, ContentModel post)
    {
        var no = ForumContentHandler.UsesTopicComments(rule.Type) && post.LevelNo > 0 ? post.TargetNo : post.ContentNo;
        return rule.Type == ContentTypes.FAQ ? $"/content/{rule.RuleNo}" : $"/content/{rule.RuleNo}/view/{no}";
    }
}
