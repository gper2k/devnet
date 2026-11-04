using BitPlus.Data.Content;
using BitPlus.Web.Site.Data;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// "공지" 위젯 로더: 고른 게시판(비우면 전체)의 볼 수 있는 공지글 최신순 (설정: ruleNos, count).
/// </summary>
public class NoticeLoader : BoardWidgetLoader
{
    /// <summary>
    /// <see cref="NoticeLoader"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    public NoticeLoader(SiteDbContext db) : base(db) { }

    /// <inheritdoc />
    public override string Widget => "Notice";

    /// <inheritdoc />
    protected override IQueryable<ContentModel> Refine(IQueryable<ContentModel> query) => query.Where(p => p.IsNotice);
}
