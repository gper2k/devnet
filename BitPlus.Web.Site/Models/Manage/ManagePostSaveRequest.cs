namespace BitPlus.Web.Site.Models.Manage;

/// <summary>
/// AI 에이전트 및 외부 관리용 글 저장(등록/수정) 요청 DTO.
/// </summary>
public class ManagePostSaveRequest
{
    /// <summary>게시판 규칙 번호</summary>
    public int RuleNo { get; set; }

    /// <summary>글 번호 (수정 시 필수, 신규 등록 시 null)</summary>
    public long? ContentNo { get; set; }

    /// <summary>글 제목</summary>
    public string Title { get; set; }

    /// <summary>글 본문 (HTML 또는 Markdown)</summary>
    public string Body { get; set; }

    /// <summary>분류/카테고리</summary>
    public string? Category { get; set; }

    /// <summary>태그 목록</summary>
    public List<string>? Tags { get; set; }

    /// <summary>연관 링크 목록</summary>
    public List<string>? Links { get; set; }

    /// <summary>공지글 여부</summary>
    public bool IsNotice { get; set; }

    /// <summary>발행 여부 (true: 즉시 게시, false: 임시 저장)</summary>
    public bool Published { get; set; }

    /// <summary>원문 언어(문화권, 예: ko·en). 다국어 게시판의 새 글에만 쓰며, 비우면 요청의 화면 언어</summary>
    public string? Culture { get; set; }

    /// <summary>
    /// <see cref="ManagePostSaveRequest"/> 기본 생성자.
    /// </summary>
    public ManagePostSaveRequest()
    {
        this.RuleNo = 0;
        this.ContentNo = null;
        this.Title = string.Empty;
        this.Body = string.Empty;
        this.Category = null;
        this.Tags = [];
        this.Links = [];
        this.IsNotice = false;
        this.Published = true;
        this.Culture = null;
    }
}
