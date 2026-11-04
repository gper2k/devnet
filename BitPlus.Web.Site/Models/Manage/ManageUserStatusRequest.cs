namespace BitPlus.Web.Site.Models.Manage;

/// <summary>
/// AI 에이전트 및 외부 관리용 회원 상태 변경(활성화/잠금) 요청 DTO.
/// </summary>
public class ManageUserStatusRequest
{
    /// <summary>사용자 식별키</summary>
    public Guid UserId { get; set; }

    /// <summary>계정 활성화 여부 (On/Off 토글, null 이면 변경 안 함)</summary>
    public bool? Enabler { get; set; }

    /// <summary>계정 잠금 여부 (true: 잠금, false: 잠금 해제, null 이면 변경 안 함)</summary>
    public bool? Lockout { get; set; }

    /// <summary>계정 잠금 시 유지 시간(분 단위, 기본 60분, Lockout=true 일 때만 적용)</summary>
    public int? LockoutMinutes { get; set; }

    /// <summary>
    /// <see cref="ManageUserStatusRequest"/> 기본 생성자.
    /// </summary>
    public ManageUserStatusRequest()
    {
        this.UserId = Guid.Empty;
        this.Enabler = null;
        this.Lockout = null;
        this.LockoutMinutes = 60;
    }
}
