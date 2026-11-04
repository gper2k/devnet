namespace BitPlus.Web.Site.Models.Manage;

/// <summary>
/// AI 에이전트 및 외부 관리용 회원 역할 배정/회수 요청 DTO.
/// </summary>
public class ManageUserRoleRequest
{
    /// <summary>사용자 식별키</summary>
    public Guid UserId { get; set; }

    /// <summary>역할 식별키</summary>
    public Guid RoleId { get; set; }

    /// <summary>배정 여부 (true: 역할 부여, false: 역할 회수)</summary>
    public bool Assign { get; set; }

    /// <summary>기본 역할 여부</summary>
    public bool Default { get; set; }

    /// <summary>
    /// <see cref="ManageUserRoleRequest"/> 기본 생성자.
    /// </summary>
    public ManageUserRoleRequest()
    {
        this.UserId = Guid.Empty;
        this.RoleId = Guid.Empty;
        this.Assign = true;
        this.Default = false;
    }
}
