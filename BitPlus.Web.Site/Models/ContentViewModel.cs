namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 보기 페이지(<c>/view/{ruleNo}/{contentNo}</c>) 뷰 모델: 글 하나의 제목과 본문.
/// </summary>
public class ContentViewModel
{
    /// <summary>
    /// <see cref="ContentViewModel"/> 기본 생성자.
    /// </summary>
    public ContentViewModel()
    {
        this.RuleNo = 0;
        this.ContentNo = 0;
        this.Title = string.Empty;
        this.Body = string.Empty;
        this.CDate = DateTime.MinValue;
        this.CanEdit = false;
        this.SideHidden = true;
        this.SideRight = false;
        this.SideMenuKey = null;
        this.Culture = string.Empty;
        this.OriginalCulture = string.Empty;
        this.Translations = [];
    }

    /// <summary>컨텐츠 규칙 번호</summary>
    public int RuleNo { get; set; }

    /// <summary>규칙 안의 글 번호</summary>
    public int ContentNo { get; set; }

    /// <summary>글 제목 (페이지 제목으로만 쓰고 본문 영역에는 출력하지 않음)</summary>
    public string Title { get; set; }

    /// <summary>본문 HTML (관리자가 관리하는 컨텐츠라 정제 없이 그대로 출력)</summary>
    public string Body { get; set; }

    /// <summary>등록 시각 (UTC)</summary>
    public DateTime CDate { get; set; }

    /// <summary>현재 사용자가 이 글을 편집할 수 있는지 (관리자) — 하단 편집 버튼 표시용</summary>
    public bool CanEdit { get; set; }

    /// <summary>사이드 패널 감춤 여부 (페이지 뷰는 사이드를 보이게 저장한 페이지만 보임)</summary>
    public bool SideHidden { get; set; }

    /// <summary>사이드 패널을 본문 우측에 둘지 여부 (아니면 좌측)</summary>
    public bool SideRight { get; set; }

    /// <summary>사이드 패널 연결 메뉴 식별키 (null 이면 자동 = 컨텐츠맵 트리)</summary>
    public string? SideMenuKey { get; set; }

    /// <summary>보이는 언어(문화권) — 원문이면 <see cref="OriginalCulture"/> 와 같음</summary>
    public string Culture { get; set; }

    /// <summary>원문 언어(문화권)</summary>
    public string OriginalCulture { get; set; }

    /// <summary>번역 목록 (문화권, 언어 이름, AI 번역, 원문이 번역 뒤에 바뀜) — 있으면 언어 전환 표시</summary>
    public List<(string Culture, string Name, bool IsMachine, bool Outdated)> Translations { get; set; }
}
