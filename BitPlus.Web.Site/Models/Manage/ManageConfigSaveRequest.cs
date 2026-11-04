namespace BitPlus.Web.Site.Models.Manage;

/// <summary>
/// AI 에이전트 및 외부 관리용 환경 설정 저장 요청 DTO.
/// </summary>
public class ManageConfigSaveRequest
{
    /// <summary>설정 키</summary>
    public string Key { get; set; }

    /// <summary>설정 값 (문자열 또는 JSON 문자열)</summary>
    public string Value { get; set; }

    /// <summary>
    /// <see cref="ManageConfigSaveRequest"/> 기본 생성자.
    /// </summary>
    public ManageConfigSaveRequest()
    {
        this.Key = string.Empty;
        this.Value = string.Empty;
    }
}
