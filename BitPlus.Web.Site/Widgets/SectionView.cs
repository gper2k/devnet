namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 페이지에 배치한 영역 하나를 그릴 정보: 요소 id, 영역 종류 키·이름, 화면 폭별 grid CSS, 슬롯 목록.
/// </summary>
public class SectionView
{
    /// <summary>배치 안의 영역 id (배치 JSON 의 id)</summary>
    public string Id { get; set; }

    /// <summary>요소 id (페이지 + 영역 id, CSS 대상)</summary>
    public string ElementId { get; set; }

    /// <summary>영역 종류 키</summary>
    public string Type { get; set; }

    /// <summary>영역 종류 이름 (현재 화면 언어, 편집 모드 표시용)</summary>
    public string TypeName { get; set; }

    /// <summary>화면 폭별 grid CSS (<see cref="SectionGrid.Css"/>)</summary>
    public string Css { get; set; }

    /// <summary>슬롯 목록 (1024 이상 배치 순서)</summary>
    public List<SlotView> Slots { get; set; }

    /// <summary>
    /// <see cref="SectionView"/> 기본 생성자
    /// </summary>
    public SectionView()
    {
        this.Id = string.Empty;
        this.ElementId = string.Empty;
        this.Type = string.Empty;
        this.TypeName = string.Empty;
        this.Css = string.Empty;
        this.Slots = [];
    }
}
