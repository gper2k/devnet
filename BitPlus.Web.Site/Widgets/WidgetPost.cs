namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 게시판 위젯(최신 글·공지·갤러리)이 출력 뷰에 넘기는 글 한 줄: 제목, 글 보기 주소, 게시판 이름, 작성일(UTC), 본문 요약(일반 글자), 대표 이미지 주소.
/// </summary>
public class WidgetPost
{
    /// <summary>제목</summary>
    public string Title { get; set; }

    /// <summary>글 보기 주소</summary>
    public string Url { get; set; }

    /// <summary>게시판 이름 (현재 화면 언어)</summary>
    public string Board { get; set; }

    /// <summary>작성일 (UTC)</summary>
    public DateTime Date { get; set; }

    /// <summary>본문 요약 (일반 글자, 없으면 null)</summary>
    public string? Summary { get; set; }

    /// <summary>대표 이미지 주소 (첨부 이미지 썸네일 또는 본문 첫 이미지, 없으면 null)</summary>
    public string? Image { get; set; }

    /// <summary>
    /// <see cref="WidgetPost"/> 기본 생성자
    /// </summary>
    public WidgetPost()
    {
        this.Title = string.Empty;
        this.Url = string.Empty;
        this.Board = string.Empty;
        this.Date = DateTime.UtcNow;
        this.Summary = null;
        this.Image = null;
    }
}
