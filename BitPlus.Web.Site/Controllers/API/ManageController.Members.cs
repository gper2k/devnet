using BitPlus.Web.Site.Models.Manage;
using Microsoft.AspNetCore.Mvc;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ManageController
{
    #region Members (MembershipController 및 AccountManager 위임)

    /// <summary>
    /// 회원(사용자) 목록을 검색어 및 페이징 조건으로 조회합니다.
    /// <see cref="MembershipController.GetUserList"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="keyword">검색어 (ID, 이메일, 표시명 등)</param>
    /// <param name="page">페이지 번호 (1부터 시작, 기본 1)</param>
    /// <param name="pageSize">페이지 크기 (기본 10, 최대 100)</param>
    /// <returns>회원 목록 및 총 건수</returns>
    [HttpGet("members")]
    public Task<IActionResult> ListMembers([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => this.Membership.GetUserList(keyword, page, pageSize);

    /// <summary>
    /// 단일 회원의 상세 정보를 조회합니다.
    /// <see cref="MembershipController.GetUser"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="userId">사용자 식별키</param>
    /// <returns>회원 상세 정보</returns>
    [HttpGet("members/{userId:guid}")]
    public Task<IActionResult> GetMember(Guid userId) => this.Membership.GetUser(userId);

    /// <summary>
    /// 회원을 삭제합니다.
    /// <see cref="MembershipController.DeleteUser"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="userId">삭제할 사용자 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("members/{userId:guid}")]
    public Task<IActionResult> DeleteMember(Guid userId) => this.Membership.DeleteUser(userId);

    /// <summary>
    /// 회원의 활성화 상태(Enabler) 또는 잠금(Lockout) 상태를 변경합니다.
    /// 활성화 여부는 <see cref="MembershipController.SetUserEnabler"/>에 위임합니다.
    /// </summary>
    /// <param name="request">상태 변경 요청 모델</param>
    /// <returns>처리 결과</returns>
    [HttpPost("members/status")]
    public async Task<IActionResult> SetMemberStatus([FromBody] ManageUserStatusRequest request)
    {
        if (request.Enabler.HasValue)
        {
            var result = await this.Membership.SetUserEnabler(request.UserId, request.Enabler.Value);
            if (result is not OkObjectResult && result is not StatusCodeResult { StatusCode: 200 }) return result;
        }

        if (request.Lockout.HasValue)
        {
            var user = await this.AccountManager!.GetUserDetailAsync(request.UserId);
            if (user is null) return this.NotFoundResult();
            if (user.IsSystem) return this.Error(this.Lang("SystemChangeWarning"));

            user.LockoutEnd = request.Lockout.Value
                ? DateTimeOffset.UtcNow.AddMinutes(request.LockoutMinutes.GetValueOrDefault(60))
                : null;
            user.UDate = DateTime.UtcNow;
            await this.AccountManager.UpdateUserAsync(user);
        }

        return this.Complete();
    }

    /// <summary>
    /// 사이트에 등록된 역할 목록을 조회합니다.
    /// <see cref="MembershipController.GetRoleList"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>역할 목록</returns>
    [HttpGet("members/roles")]
    public Task<IActionResult> GetRoles([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => this.Membership.GetRoleList(keyword, page, pageSize, false);

    #endregion
}
