namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 포탈(<c>/content</c>, 메뉴별 포탈 <c>/content/menu/{메뉴 키}</c>) 뷰 모델.
/// </summary>
public class ContentPortalModel
{
    /// <summary>
    /// <see cref="ContentPortalModel"/> 기본 생성자.
    /// </summary>
    public ContentPortalModel()
    {
        this.Items = [];
        this.MenuKey = null;
    }

    /// <summary>메뉴별 포탈의 메뉴 키 (<c>/content/menu/{메뉴 키}</c>, 기본 포탈이면 null) — 위젯 배치 portal@{메뉴 키}, 사이드 패널 연결 메뉴</summary>
    public string? MenuKey { get; set; }

    /// <summary>나열할 컨텐츠 규칙 (규칙 번호 순)</summary>
    public List<ContentPortalItem> Items { get; set; }
}
