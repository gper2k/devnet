using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.HTTP.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 사용자 Web API 컨트롤러 (api/user). 작성글 보기(사용자가 쓴 글·댓글 목록, 이 파일), 내 정보(<c>UserController.Me.cs</c>: 활성 그룹·정보 편집·이미지·환경설정), 메시지(<c>UserController.Message.cs</c>)를 제공합니다.
/// 로그인 사용자만 쓸 수 있고, 목록에는 지금 사용자가 열 수 있는 글(<see cref="PostAccessRule"/>)과 그 글에 단 댓글만 담깁니다. 익명 게시판의 글·댓글은 그 게시판 관리자가 아니면 빠집니다.
/// </summary>
public partial class UserController : APIControllerBase
{
    /// <summary>목록을 만들 때 살펴보는 최근 글·댓글의 최대 수 (각각)</summary>
    private const int MaxScan = 500;

    /// <summary>메시지 최대 글자 수</summary>
    private const int MaxMessage = 1000;

    /// <summary>같은 사용자의 메시지 발송 최소 간격 (연속 발송 제한)</summary>
    private static readonly TimeSpan MessageGap = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <see cref="UserController"/> 기본 생성자.
    /// </summary>
    public UserController() : base() { }

    /// <summary>
    /// 사용자가 쓴 게시물(포럼 댓글 제외) 또는 댓글(일반 댓글 + 포럼 댓글)을 최신순으로 페이지 단위 조회합니다.
    /// </summary>
    /// <param name="userId">사용자 식별키</param>
    /// <param name="kind"><c>posts</c>(게시물, 기본) 또는 <c>comments</c>(댓글)</param>
    /// <param name="page">페이지 번호 (0부터)</param>
    /// <param name="size">페이지 크기 (1~50)</param>
    /// <returns>postTotal, commentTotal, total(이 종류의 수), items([{board, title, excerpt(댓글 내용 일부), url, cDate}])</returns>
    [HttpGet("{userId:guid}/activity")]
    public async Task<IActionResult> Activity(Guid userId, [FromQuery] string? kind, [FromQuery] int page = 0, [FromQuery] int size = 10)
    {
        if (!this.IsAuthenticated) return this.UnauthorizedResult();
        var (posts, comments, postTotal, commentTotal) = await this.ActivityOfAsync(userId, kind);
        var items = kind == "comments" ? comments : posts;
        var total = kind == "comments" ? commentTotal : postTotal;
        size = Math.Clamp(size, 1, 50);
        return this.Complete(new { postTotal, commentTotal, total, items = items.Skip(Math.Max(page, 0) * size).Take(size) });
    }

    /// <summary>
    /// 사용자의 게시물·댓글 중 지금 사용자가 볼 수 있는 것을 최신순으로 모읍니다 (각각 최근 <see cref="MaxScan"/> 개 안에서).
    /// 게시물·포럼 댓글은 그 글을, 일반 댓글은 댓글을 단 글을 <see cref="PostAccessRule.Check"/> 로 판정합니다 (포럼 댓글은 소속 토픽도).
    /// ponytail: 최근 500개씩만 살펴봄 — 활동이 아주 많은 사용자의 오래된 글은 빠짐, 필요하면 DB 쪽 페이지 처리로
    /// </summary>
    private async Task<(List<object> Posts, List<object> Comments, int PostTotal, int CommentTotal)> ActivityOfAsync(Guid userId, string? kind)
    {
        var db = this.DbContext!;
        var (viewer, now) = (this.CurrentUserAssets, DateTime.UtcNow);
        var isPolicyAdmin = this.IsManager || this.IsAllowed(WebConfig.ContentManagerPolicyName);
        var rules = (await db.ContentRules.AsNoTracking().Include(p => p.Title).Include(p => p.AccessRule).Include(p => p.Approvers).Include(p => p.Properties).Where(p => p.Enabler && !p.OnlyView).ToListAsync())
            .Select(rule => (Rule: rule, Permission: ContentPermission.Resolve(rule, viewer, isPolicyAdmin)))
            .Where(p => p.Permission.IsAdmin || !DefaultContentHandler.Option(p.Rule, "UseAnonymous")) // 익명 게시판은 작성자를 드러내지 않음
            .ToDictionary(p => p.Rule.Id);
        var ruleIds = rules.Keys.ToList();
        bool CanOpen(Guid ruleId, Guid? writerId, bool visible, bool published, bool? approved, DateTime? start, DateTime? end, bool isPrivate, IEnumerable<(AccountTypes TargetType, Guid TargetKey)> readRules)
        {
            if (!rules.TryGetValue(ruleId, out var r)) return false;
            var (rule, perm) = (r.Rule, r.Permission);
            if (perm.IsAdmin || PostAccessRule.IsApprover(rule, viewer?.UserId) || (writerId != null && writerId == viewer?.UserId)) return true;
            if (!perm.CanRead) return false;
            if (!(visible && published && (rule.Approvers.Count == 0 || approved == true) && (start == null || start <= now) && (rule.Type == ContentTypes.Survey || end == null || end >= now))) return false;
            if (DefaultContentHandler.Option(rule, "UsePrivate") && isPrivate) return false;
            if (readRules.Any() && (viewer is null || !readRules.Any(p => ContentPermission.Matches(p.TargetType, p.TargetKey, viewer)))) return false;
            return true;
        }
        string BoardOf(Guid ruleId) => this.Request.DisplayName(rules[ruleId].Rule.Title, rules[ruleId].Rule.RuleNo.ToString());
        string UrlOf(Guid ruleId, int? contentNo) => $"/content/{rules[ruleId].Rule.RuleNo}/view/{contentNo}";
        static string Excerpt(string? text) => (text ?? string.Empty).Trim() is var plain && plain.Length > 120 ? plain[..120] + "…" : (text ?? string.Empty).Trim(); // 일반 댓글은 글자 그대로 저장돼 있음
        static string HtmlExcerpt(string? html) => Excerpt(System.Net.WebUtility.HtmlDecode(html.ClearHtml() ?? string.Empty)); // 포럼 댓글 본문은 HTML

        var needPosts = kind != "comments";
        var needComments = kind == "comments";

        var posts = new List<object>();
        var comments = new List<object>();

        var written = await db.Contents.AsNoTracking()
            .Where(p => p.WriterId == userId && p.ContentNo != null && ruleIds.Contains(p.ContentRuleId))
            .OrderByDescending(p => p.CDate)
            .Take(MaxScan)
            .Select(p => new
            {
                p.Id,
                p.ContentRuleId,
                p.ContentNo,
                p.Title,
                p.CDate,
                p.Visible,
                p.Published,
                p.Approved,
                p.Start,
                p.End,
                p.WriterId,
                p.TargetNo,
                p.LevelNo,
                ReadRules = p.ReadRule.Select(r => new { r.TargetType, r.TargetKey }).ToList(),
                IsPrivate = p.Properties.Any(pr => pr.Key == "IsPrivate" && pr.Value == "true"),
                ContentBody = (p.LevelNo ?? 0) > 0 ? p.ContentBody : null
            })
            .ToListAsync();
        bool IsForumReply(Guid ruleId, int? levelNo) => rules.TryGetValue(ruleId, out var r) && ForumContentHandler.UsesTopicComments(r.Rule.Type) && (levelNo ?? 0) > 0;

        var postRows = written.Where(p => !IsForumReply(p.ContentRuleId, p.LevelNo) && CanOpen(p.ContentRuleId, p.WriterId, p.Visible, p.Published, p.Approved, p.Start, p.End, p.IsPrivate, p.ReadRules.Select(r => (r.TargetType, r.TargetKey)))).ToList();
        var postTotal = postRows.Count;
        if (needPosts)
            posts = [.. postRows.Select(p => (object)new { board = BoardOf(p.ContentRuleId), title = p.Title, excerpt = string.Empty, url = UrlOf(p.ContentRuleId, p.ContentNo), cDate = p.CDate })];

        // 포럼 댓글: 소속 토픽(같은 규칙의 TargetNo 글)도 열 수 있어야 함
        var replies = written.Where(p => IsForumReply(p.ContentRuleId, p.LevelNo)).ToList();
        var topicKeys = replies.Select(p => p.TargetNo).Distinct().ToList();
        var replyRuleIds = replies.Select(p => p.ContentRuleId).Distinct().ToList();
        var topics = (replies.Count == 0 || !needComments) ? [] : await db.Contents.AsNoTracking()
            .Where(p => replyRuleIds.Contains(p.ContentRuleId) && topicKeys.Contains(p.ContentNo) && (p.LevelNo ?? 0) == 0)
            .Select(p => new
            {
                p.ContentRuleId,
                p.ContentNo,
                p.Title,
                p.Visible,
                p.Published,
                p.Approved,
                p.Start,
                p.End,
                p.WriterId,
                ReadRules = p.ReadRule.Select(r => new { r.TargetType, r.TargetKey }).ToList(),
                IsPrivate = p.Properties.Any(pr => pr.Key == "IsPrivate" && pr.Value == "true")
            })
            .ToListAsync();

        var topicMap = topics.ToDictionary(t => (t.ContentRuleId, t.ContentNo));
        var forumComments = new List<(DateTime Date, object Item)>();
        if (needComments)
        {
            foreach (var rep in replies)
            {
                if (topicMap.TryGetValue((rep.ContentRuleId, rep.TargetNo), out var topic)
                    && CanOpen(rep.ContentRuleId, rep.WriterId, rep.Visible, rep.Published, rep.Approved, rep.Start, rep.End, rep.IsPrivate, rep.ReadRules.Select(r => (r.TargetType, r.TargetKey)))
                    && CanOpen(topic.ContentRuleId, topic.WriterId, topic.Visible, topic.Published, topic.Approved, topic.Start, topic.End, topic.IsPrivate, topic.ReadRules.Select(r => (r.TargetType, r.TargetKey))))
                {
                    forumComments.Add((rep.CDate, new { board = BoardOf(rep.ContentRuleId), title = topic.Title, excerpt = HtmlExcerpt(rep.ContentBody), url = UrlOf(rep.ContentRuleId, rep.ContentNo), cDate = rep.CDate }));
                }
            }
        }

        // 일반 댓글: 댓글을 단 글을 열 수 있어야 함
        var commentTotal = replies.Count;
        if (needComments)
        {
            var rows = await db.Comments.AsNoTracking().Where(p => p.CommentUserId == userId && ruleIds.Contains(p.Content!.ContentRuleId)).OrderByDescending(p => p.CDate).Take(MaxScan)
                .Select(p => new { p.ContentId, p.CommentBody, p.CDate }).ToListAsync();
            var parentIds = rows.Select(p => p.ContentId).Distinct().ToList();
            var parents = rows.Count == 0 ? [] : (await db.Contents.AsNoTracking().Where(p => parentIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.ContentRuleId,
                    p.ContentNo,
                    p.Title,
                    p.Visible,
                    p.Published,
                    p.Approved,
                    p.Start,
                    p.End,
                    p.WriterId,
                    ReadRules = p.ReadRule.Select(r => new { r.TargetType, r.TargetKey }).ToList(),
                    IsPrivate = p.Properties.Any(pr => pr.Key == "IsPrivate" && pr.Value == "true")
                }).ToListAsync()).ToDictionary(p => p.Id);

            foreach (var row in rows)
            {
                if (parents.TryGetValue(row.ContentId, out var parent)
                    && CanOpen(parent.ContentRuleId, parent.WriterId, parent.Visible, parent.Published, parent.Approved, parent.Start, parent.End, parent.IsPrivate, parent.ReadRules.Select(r => (r.TargetType, r.TargetKey))))
                {
                    forumComments.Add((row.CDate, new { board = BoardOf(parent.ContentRuleId), title = parent.Title, excerpt = Excerpt(row.CommentBody), url = UrlOf(parent.ContentRuleId, parent.ContentNo), cDate = row.CDate }));
                }
            }
            comments = [.. forumComments.OrderByDescending(p => p.Date).Select(p => p.Item)];
            commentTotal = comments.Count;
        }
        else
        {
            commentTotal = replies.Count + await db.Comments.AsNoTracking().CountAsync(p => p.CommentUserId == userId && ruleIds.Contains(p.Content!.ContentRuleId));
        }

        return (posts, comments, postTotal, commentTotal);
    }
}
