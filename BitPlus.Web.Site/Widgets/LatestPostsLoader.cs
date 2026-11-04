using BitPlus.Web.Site.Data;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// "최신 글" 위젯 로더: 고른 게시판(비우면 전체)의 볼 수 있는 원본 글 최신순 (설정: ruleNos, count).
/// </summary>
public class LatestPostsLoader : BoardWidgetLoader
{
    /// <summary>
    /// <see cref="LatestPostsLoader"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    public LatestPostsLoader(SiteDbContext db) : base(db) { }

    /// <inheritdoc />
    public override string Widget => "LatestPosts";
}
