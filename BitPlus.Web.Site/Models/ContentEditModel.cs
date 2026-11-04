namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 작성/수정 페이지(<c>/view/edit/{contentNo?}</c>) 뷰 모델.
/// </summary>
public class ContentEditModel
{
    /// <summary>
    /// <see cref="ContentEditModel"/> 기본 생성자.
    /// </summary>
    public ContentEditModel()
    {
        this.ContentNo = ContentEditModel.NewNo;
        this.OwnerKey = string.Empty;
        this.Title = string.Empty;
        this.Body = string.Empty;
    }

    /// <summary>신규 작성을 뜻하는 글 번호 (주소에 글 번호가 없을 때)</summary>
    public const int NewNo = -1;

    /// <summary>글 번호 (신규 작성이면 <see cref="NewNo"/>)</summary>
    public int ContentNo { get; set; }

    /// <summary>신규 작성 여부</summary>
    public bool IsNew => this.ContentNo == NewNo;

    /// <summary>에디터 이미지 소유자 키 (수정이면 글 식별키, 신규면 작성 중 임시 키 — 저장 때 글 식별키로 이전)</summary>
    public string OwnerKey { get; set; }

    /// <summary>제목 (보기 화면의 페이지 제목, 본문에는 나오지 않음)</summary>
    public string Title { get; set; }

    /// <summary>본문 HTML</summary>
    public string Body { get; set; }
}
