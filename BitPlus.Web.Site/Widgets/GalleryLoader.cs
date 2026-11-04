using BitPlus.Data.Content;
using BitPlus.Web.Site.Data;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// "갤러리" 위젯 로더: 고른 갤러리 게시판(비우면 모든 갤러리)의 볼 수 있는 글 최신순과 대표 이미지 (설정: ruleNos, count). 이미지가 없는 글도 제목으로 나옵니다.
/// </summary>
public class GalleryLoader : BoardWidgetLoader
{
    /// <summary>
    /// <see cref="GalleryLoader"/> 생성자.
    /// </summary>
    /// <param name="db">사이트 DB (요청 단위)</param>
    public GalleryLoader(SiteDbContext db) : base(db) { }

    /// <inheritdoc />
    public override string Widget => "Gallery";

    /// <inheritdoc />
    protected override ContentTypes[]? Types => [ContentTypes.Gallery];

    /// <inheritdoc />
    protected override bool NeedImage(WidgetContext context) => true;
}
