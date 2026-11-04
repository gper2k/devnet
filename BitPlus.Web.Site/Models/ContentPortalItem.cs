using BitPlus.Data.Content;

namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 포탈(<c>/content</c>)에 나열하는 컨텐츠 규칙 하나.
/// </summary>
public class ContentPortalItem
{
    /// <summary>
    /// <see cref="ContentPortalItem"/> 기본 생성자.
    /// </summary>
    public ContentPortalItem()
    {
        this.RuleNo = 0;
        this.Title = string.Empty;
        this.Type = ContentTypes.Default;
        this.Count = 0;
    }

    /// <summary>컨텐츠 규칙 번호 (링크는 /content/{RuleNo})</summary>
    public int RuleNo { get; set; }

    /// <summary>현재 언어 규칙으로 고른 규칙 제목</summary>
    public string Title { get; set; }

    /// <summary>컨텐츠 형식</summary>
    public ContentTypes Type { get; set; }

    /// <summary>등록된 글 수</summary>
    public int Count { get; set; }
}
