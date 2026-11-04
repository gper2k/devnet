namespace BitPlus.Web.Site.Models.Manage;

/// <summary>
/// AI 에이전트 및 외부 관리용 메뉴 등록/수정 요청 DTO.
/// </summary>
public class ManageMenuSaveRequest
{
    /// <summary>메뉴 식별키 (영문, 숫자, _ - . 1~64자)</summary>
    public string Key { get; set; }

    /// <summary>상위 메뉴 식별키 (없으면 최상위 메뉴)</summary>
    public string? ParentKey { get; set; }

    /// <summary>기본 표시명</summary>
    public string Name { get; set; }

    /// <summary>다국어 명칭 사전 ({문화권: 명칭})</summary>
    public Dictionary<string, string>? DpNames { get; set; }

    /// <summary>아이콘 태그 HTML 또는 클래스</summary>
    public string? Icon { get; set; }

    /// <summary>메뉴 형식 (Link, Board, Page, Group 등)</summary>
    public string? Type { get; set; }

    /// <summary>이동 경로 또는 액션 URL</summary>
    public string? Action { get; set; }

    /// <summary>확장 속성 값</summary>
    public string? Extend { get; set; }

    /// <summary>사용 여부 (On/Off 토글)</summary>
    public bool Enabler { get; set; }

    /// <summary>표시 여부 (사이트 메뉴 네비게이션에 노출 여부)</summary>
    public bool Visible { get; set; }

    /// <summary>정렬 순서</summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// <see cref="ManageMenuSaveRequest"/> 기본 생성자.
    /// </summary>
    public ManageMenuSaveRequest()
    {
        this.Key = string.Empty;
        this.ParentKey = null;
        this.Name = string.Empty;
        this.DpNames = [];
        this.Icon = null;
        this.Type = "Link";
        this.Action = null;
        this.Extend = null;
        this.Enabler = true;
        this.Visible = true;
        this.SortOrder = 0;
    }
}
