using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Data.CommonModel;
using BitPlus.Web.Security.Validation;
using BitPlus.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using BitPlus.Data.Converter;
using System.Globalization;
using BitPlus.Web.Data;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 범용 계정 Web API 컨트롤러: 관리 화면용 그룹/사용자/역할, 가입 약관, 메일 템플릿, 계정 구성 관리와 회원 가입(인증 메일 발송/재발송).
/// 요청 데이터는 모델 클래스 없이 개별 폼 필드로 받으며, 다국어 표시명/속성처럼 사전 형태 값은 JSON 문자열 필드, 목록은 반복 필드로 받습니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public partial class MembershipController : APIControllerBase
{
    /// <summary>
    /// <see cref="MembershipController"/> 기본 생성자.
    /// </summary>
    public MembershipController() : base() { }

    #region Group

    /// <summary>
    /// 전체 부서 조직도를 트리 구조(key/text/sort/system/userCount/child)로 조회합니다.
    /// 같은 부모 아래 형제는 정렬 등급 코드(Grade) 순, 코드가 없으면 "zzzz"로 간주해 맨 아래, 동순위는 이름 순입니다.
    /// 숨김(Visible=false) 그룹을 제외하면 그 하위 그룹도 함께 제외됩니다.
    /// </summary>
    /// <param name="includeHidden">숨김 그룹 포함 여부 (기본 true)</param>
    /// <returns>최상위 부서부터 중첩된 트리 노드 목록</returns>
    [HttpGet("group/tree")]
    public async Task<IActionResult> GetGroupTree([FromQuery] bool includeHidden = true)
    {
        var groups = await this.AccountManager!.GetAllGroupsAsync();
        var counts = await this.CountGroupUsersAsync();
        var byParent = groups
            .Where(p => includeHidden || p.Visible)
            .OrderBy(this.SortKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Name)
            .ToLookup(p => p.ParentId);

        object BuildNode(AppGroup group) => new
        {
            key = group.Id,
            text = this.GroupText(group),
            sort = this.SortKey(group),
            system = group.IsSystem,
            userCount = counts.GetValueOrDefault(group.Id),
            child = byParent[group.Id].Select(BuildNode).ToList()
        };

        return this.Complete(byParent[null].Select(BuildNode).ToList());
    }

    /// <summary>
    /// 부서 편집 정보를 조회합니다. 시스템 속성 키는 "@" 접두로, 역할은 표시명 규칙 텍스트로 내려줍니다.
    /// </summary>
    /// <param name="id">부서 식별키</param>
    /// <returns>부서 편집 정보</returns>
    [HttpGet("group/{id:guid}")]
    public async Task<IActionResult> GetGroup(Guid id)
    {
        var group = await this.AccountManager!.GetGroupDetailAsync(id);
        if (group is null) return this.NotFoundResult();

        var edit = group.ToEditModel();
        edit.Roles = (await this.AccountManager!.GetRolesAsync(edit.Roles.Keys)).ToDictionary(p => p.Id, this.RoleText);
        return this.Complete(edit);
    }

    /// <summary>
    /// 새 부서를 생성합니다.
    /// </summary>
    /// <param name="name">그룹명 (생성 후 변경 불가)</param>
    /// <param name="grade">정렬 등급 코드 (최대 4자)</param>
    /// <param name="description">설명</param>
    /// <param name="parentId">상위 그룹 식별키</param>
    /// <param name="managerId">담당자 식별키</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="inherit">상위 그룹 상속 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <param name="properties">일반 속성 JSON ({키: 값})</param>
    /// <param name="roles">역할 식별키 목록 (반복 필드)</param>
    /// <param name="defaultRoleId">기본 역할 식별키</param>
    /// <returns>생성된 그룹의 트리 노드(key/text/sort/parentId)</returns>
    [HttpPost("group")]
    [Valid("GroupSave")]
    public async Task<IActionResult> CreateGroup(
        [FromForm] string? name, [FromForm] string? grade, [FromForm] string? description, [FromForm] Guid? parentId, [FromForm] Guid? managerId,
        [FromForm] bool visible, [FromForm] bool inherit, [FromForm] string? dpNames, [FromForm] string? properties,
        [FromForm] List<Guid>? roles, [FromForm] Guid? defaultRoleId)
    {
        var group = this.ToGroup(null, name ?? string.Empty, grade, description, parentId, managerId, visible, inherit, dpNames, properties, roles, defaultRoleId);
        try
        {
            await this.AccountManager!.CreateGroupAsync(group);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete(await this.ToTreeNodeAsync(group), WebResources.AddedComplete);
    }

    /// <summary>
    /// 기존 부서 정보를 수정합니다. 그룹명은 변경되지 않으며, 상위 부서 변경 시 하위 부서 Location도 함께 재계산됩니다.
    /// </summary>
    /// <param name="id">수정할 부서 식별키</param>
    /// <param name="name">그룹명 (무시됨)</param>
    /// <param name="grade">정렬 등급 코드 (최대 4자)</param>
    /// <param name="description">설명</param>
    /// <param name="parentId">상위 그룹 식별키</param>
    /// <param name="managerId">담당자 식별키</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="inherit">상위 그룹 상속 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <param name="properties">일반 속성 JSON ({키: 값})</param>
    /// <param name="roles">역할 식별키 목록 (반복 필드)</param>
    /// <param name="defaultRoleId">기본 역할 식별키</param>
    /// <returns>수정된 그룹의 트리 노드(key/text/sort/parentId)</returns>
    [HttpPut("group/{id:guid}")]
    [Valid("GroupSave")]
    public async Task<IActionResult> UpdateGroup(Guid id,
        [FromForm] string? name, [FromForm] string? grade, [FromForm] string? description, [FromForm] Guid? parentId, [FromForm] Guid? managerId,
        [FromForm] bool visible, [FromForm] bool inherit, [FromForm] string? dpNames, [FromForm] string? properties,
        [FromForm] List<Guid>? roles, [FromForm] Guid? defaultRoleId)
    {
        var group = this.ToGroup(id, name ?? string.Empty, grade, description, parentId, managerId, visible, inherit, dpNames, properties, roles, defaultRoleId);
        try
        {
            await this.AccountManager!.UpdateGroupAsync(group);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete(await this.ToTreeNodeAsync(group), WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 부서의 화면 표시 여부(Visible)만 가볍게 전환합니다.
    /// </summary>
    /// <param name="id">부서 식별키</param>
    /// <param name="visible">전환할 표시 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("group/{id:guid}/visible")]
    public async Task<IActionResult> SetGroupVisible(Guid id, [FromForm] bool visible)
    {
        try
        {
            await this.AccountManager!.SetGroupVisibleAsync(id, visible);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFoundResult(ex.Message);
        }
        return this.Complete();
    }

    /// <summary>
    /// 부서를 삭제합니다. 시스템 그룹(하위 포함)은 거부되며, 삭제되는 그룹의 사용자 소속 연결도 함께 삭제됩니다.
    /// </summary>
    /// <param name="id">삭제할 부서 식별키</param>
    /// <param name="withChild">하위 그룹 포함 삭제 여부 (false 면 직속 하위 그룹을 상위로 올림)</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("group/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, [FromQuery] bool withChild = false)
    {
        try
        {
            await this.AccountManager!.DeleteGroupAsync(id, withChild);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 그룹 구성원 목록을 조회합니다. 선택 그룹의 하위 그룹과 소속 사용자를 한 목록(그룹 → 사용자 순)으로 묶어 페이지 단위로 내려주며,
    /// 그룹을 지정하지 않으면 전체 사용자 목록입니다. 텍스트는 다국어 출력 기본 규칙을 따릅니다.
    /// </summary>
    /// <param name="groupId">선택 그룹 식별키 (없으면 전체 사용자)</param>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (0부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <param name="includeHidden">숨김 항목 포함 여부</param>
    /// <param name="includeDisabled">비활성 사용자 포함 여부</param>
    /// <param name="root">그룹 미지정 시 최상위 그룹(상위 없음) + 어떤 그룹에도 속하지 않은 사용자만 조회 (트리에서 아무것도 선택하지 않은 상태)</param>
    /// <returns>items(type/key/text/sub/visible/enabler/cDate), total</returns>
    [HttpGet("member/list")]
    public async Task<IActionResult> GetMemberList([FromQuery] Guid? groupId, [FromQuery] string? keyword, [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10, [FromQuery] bool includeHidden = true, [FromQuery] bool includeDisabled = true, [FromQuery] bool root = false)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (groups, users, total) = await this.AccountManager!.SearchMembersAsync(groupId, keyword, includeHidden, includeDisabled, Math.Max(page, 0), pageSize, root);

        var items = groups.Select(p => new { type = "group", key = p.Id, text = this.GroupText(p), sub = p.Name, visible = p.Visible, enabler = (bool?)null, cDate = p.CDate })
            .Concat(users.Select(p => new { type = "user", key = p.Id, text = this.Request.DisplayName(p.DpNames, p.UserName), sub = p.UserName ?? string.Empty, visible = p.Visible, enabler = (bool?)p.Enabler, cDate = p.CDate }));
        return this.Complete(new { items, total });
    }

    #endregion

    #region Role

    /// <summary>
    /// 역할 목록을 검색어(역할명/표시명)로 검색해 페이지 단위로 조회합니다. 검색어가 없으면 전체 목록입니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items(key/text), total, page, pageSize</returns>
    [HttpGet("role/list")]
    public async Task<IActionResult> GetRoleList([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeHidden = false)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await this.AccountManager!.SearchRolesAsync(keyword, page, pageSize, includeHidden);
        return this.Complete(new
        {
            items = items.Select(p => new { key = p.Id, text = this.RoleText(p), sub = p.Name, description = p.Description, system = p.IsSystem, visible = p.Visible }),
            total, page, pageSize
        });
    }

    /// <summary>
    /// 역할 편집 정보를 조회합니다.
    /// </summary>
    /// <param name="id">역할 식별키</param>
    /// <returns>역할 편집 정보</returns>
    [HttpGet("role/{id:guid}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var role = await this.AccountManager!.GetRoleDetailAsync(id);
        if (role is null) return this.NotFoundResult();
        return this.Complete(new
        {
            id = role.Id, name = role.Name, text = this.RoleText(role), grade = role.Grade, description = role.Description,
            visible = role.Visible, isSystem = role.IsSystem, dpNames = role.DpNames.ToDictionary(p => p.Culture, p => p.Name)
        });
    }

    /// <summary>
    /// 새 역할을 생성합니다.
    /// </summary>
    /// <param name="name">역할명 (생성 후 변경 불가)</param>
    /// <param name="grade">정렬 등급 코드 (최대 4자)</param>
    /// <param name="description">설명</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <returns>생성된 역할 key/text</returns>
    [HttpPost("role")]
    [Valid("RoleSave")]
    public async Task<IActionResult> CreateRole([FromForm] string? name, [FromForm] string? grade, [FromForm] string? description,
        [FromForm] bool visible, [FromForm] string? dpNames)
    {
        var role = this.ToRole(null, name ?? string.Empty, grade, description, visible, dpNames);
        try
        {
            await this.AccountManager!.CreateRoleAsync(role);
        }
        catch (InvalidOperationException)
        {
            return this.Error(WebResources.ValidAlreadyItem.Formats(this.Request.GetLanguage("RoleName") ?? "Role Name")!); // 생성 시 유일한 실패: 역할명 중복
        }
        return this.Complete(new { key = role.Id, text = this.RoleText(role) }, WebResources.AddedComplete);
    }

    /// <summary>
    /// 역할 정보를 수정합니다. 역할명은 변경되지 않으며, 시스템 역할은 표시명/정렬 코드/설명만 반영됩니다.
    /// </summary>
    /// <param name="id">수정할 역할 식별키</param>
    /// <param name="name">역할명 (무시됨)</param>
    /// <param name="grade">정렬 등급 코드 (최대 4자)</param>
    /// <param name="description">설명</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <returns>수정된 역할 key/text</returns>
    [HttpPut("role/{id:guid}")]
    [Valid("RoleSave")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromForm] string? name, [FromForm] string? grade, [FromForm] string? description,
        [FromForm] bool visible, [FromForm] string? dpNames)
    {
        var role = this.ToRole(id, name ?? string.Empty, grade, description, visible, dpNames);
        try
        {
            await this.AccountManager!.UpdateRoleAsync(role);
        }
        catch (InvalidOperationException)
        {
            return this.NotFoundResult(WebResources.NotFoundData); // 수정 시 유일한 실패: 대상 없음
        }
        return this.Complete(new { key = role.Id, text = this.RoleText(role) }, WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 역할을 삭제합니다. 시스템 역할은 거부되며, 이 역할이 부여된 사용자/그룹 연결도 함께 삭제됩니다.
    /// </summary>
    /// <param name="id">삭제할 역할 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("role/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await this.AccountManager!.GetRoleDetailAsync(id);
        if (role is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (role.IsSystem) return this.Error(WebResources.SystemDelWarning);

        await this.AccountManager!.DeleteRoleAsync(id);
        return this.Complete(message: WebResources.DeletedComplete);
    }

    #endregion

    #region User

    /// <summary>
    /// 사용자 목록을 검색어(ID/이메일/표시명)로 검색해 페이지 단위로 조회합니다(비활성 사용자 제외, 담당자 지정 등 선택용). 검색어가 없으면 전체 목록입니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items(key/text/sub=사용자 ID), total, page, pageSize</returns>
    [HttpGet("user/list")]
    public async Task<IActionResult> GetUserList([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (_, users, total) = await this.AccountManager!.SearchMembersAsync(null, keyword, true, false, page - 1, pageSize);
        var items = users.Select(p => new { key = p.Id, text = this.Request.DisplayName(p.DpNames, p.UserName), sub = p.UserName });
        return this.Complete(new { items, total, page, pageSize });
    }

    /// <summary>
    /// 사용자 편집 정보를 조회합니다. 비활성 사용자도 조회되며, 시스템 속성 키는 "@" 접두로, 그룹/역할은 표시명 규칙 텍스트로 내려줍니다.
    /// </summary>
    /// <param name="id">사용자 식별키</param>
    /// <returns>사용자 편집 정보</returns>
    [HttpGet("user/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await this.AccountManager!.GetUserDetailAsync(id);
        if (user is null) return this.NotFoundResult();

        return this.Complete(new
        {
            id = user.Id,
            userName = user.UserName,
            text = this.Request.DisplayName(user.DpNames, user.UserName),
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            description = user.Description,
            visible = user.Visible,
            enabler = user.Enabler,
            isSystem = user.IsSystem,
            dpNames = user.DpNames.ToDictionary(p => p.Culture, p => p.Name),
            properties = this.EditProperties(user.Properties),
            groups = user.UserGroups.ToDictionary(p => p.GroupId, p => this.Request.DisplayName(p.Group?.DpNames, p.Group?.Name)),
            defaultGroupId = (user.UserGroups.FirstOrDefault(p => p.Default) ?? user.UserGroups.FirstOrDefault())?.GroupId,
            roles = user.UserRoles.ToDictionary(p => p.RoleId, p => this.Request.DisplayName(p.Role?.DpNames, p.Role?.Name)),
            defaultRoleId = (user.UserRoles.FirstOrDefault(p => p.Default) ?? user.UserRoles.FirstOrDefault())?.RoleId
        });
    }

    /// <summary>
    /// 새 사용자를 생성합니다. 사용자 ID 최소 길이는 AccountRule.UidMinSize 로 검증하고, 초기 비밀번호는 AccountRule.DefaultPassword 규칙으로 설정합니다.
    /// 관리자가 만든 사용자는 메일 인증 완료(EmailConfirmed)로 저장합니다.
    /// </summary>
    /// <param name="userName">사용자 ID (생성 후 변경 불가)</param>
    /// <param name="email">이메일</param>
    /// <param name="phoneNumber">전화번호</param>
    /// <param name="description">설명</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="enabler">계정 활성화 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <param name="properties">일반 속성 JSON ({키: 값})</param>
    /// <param name="groups">소속 그룹 식별키 목록 (반복 필드)</param>
    /// <param name="defaultGroupId">기본 소속 그룹 식별키</param>
    /// <param name="roles">역할 식별키 목록 (반복 필드)</param>
    /// <param name="defaultRoleId">기본 역할 식별키</param>
    /// <returns>생성된 사용자 key/text</returns>
    [HttpPost("user")]
    [Valid("UserSave")]
    public async Task<IActionResult> CreateUser(
        [FromForm] string? userName, [FromForm] string? email, [FromForm] string? phoneNumber, [FromForm] string? description,
        [FromForm] bool visible, [FromForm] bool enabler, [FromForm] string? dpNames, [FromForm] string? properties,
        [FromForm] List<Guid>? groups, [FromForm] Guid? defaultGroupId, [FromForm] List<Guid>? roles, [FromForm] Guid? defaultRoleId)
    {
        var rule = this.Config?.AccountRule ?? new AccountRule();
        if ((userName ?? string.Empty).Trim().Length < rule.UidMinSize)
            return this.Error(WebResources.ValidMinLength.Formats(this.Request.GetLanguage("UserID") ?? "User ID", rule.UidMinSize.ToString())!);

        var user = this.ToUser(null, userName ?? string.Empty, email, phoneNumber, description, visible, enabler, dpNames, properties, groups, defaultGroupId, roles, defaultRoleId);
        var hasher = this.HttpContext.GetService<IPasswordHasher<AppUser>>() ?? new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, string.Format(CultureInfo.InvariantCulture, rule.DefaultPassword, user.UserName));
        user.EmailConfirmed = true; // 관리자가 만든 사용자는 메일 인증 완료로 간주 (메일 검증 On 의 로그인 차단 대상 아님)

        try
        {
            await this.AccountManager!.CreateUserAsync(user);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        this.HttpContext.Publish(ServiceEvents.MemberCreated, user.Id.ToString(), new Dictionary<string, string?> { ["by"] = "admin" });
        return this.Complete(new { key = user.Id, text = this.Request.DisplayName(user.DpNames, user.UserName) }, WebResources.AddedComplete);
    }

    /// <summary>
    /// 사용자 정보를 수정합니다. 사용자 ID 는 변경되지 않습니다.
    /// </summary>
    /// <param name="id">수정할 사용자 식별키</param>
    /// <param name="userName">사용자 ID (무시됨)</param>
    /// <param name="email">이메일</param>
    /// <param name="phoneNumber">전화번호</param>
    /// <param name="description">설명</param>
    /// <param name="visible">표시 여부</param>
    /// <param name="enabler">계정 활성화 여부</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <param name="properties">일반 속성 JSON ({키: 값})</param>
    /// <param name="groups">소속 그룹 식별키 목록 (반복 필드)</param>
    /// <param name="defaultGroupId">기본 소속 그룹 식별키</param>
    /// <param name="roles">역할 식별키 목록 (반복 필드)</param>
    /// <param name="defaultRoleId">기본 역할 식별키</param>
    /// <returns>수정된 사용자 key/text</returns>
    [HttpPut("user/{id:guid}")]
    [Valid("UserSave")]
    public async Task<IActionResult> UpdateUser(Guid id,
        [FromForm] string? userName, [FromForm] string? email, [FromForm] string? phoneNumber, [FromForm] string? description,
        [FromForm] bool visible, [FromForm] bool enabler, [FromForm] string? dpNames, [FromForm] string? properties,
        [FromForm] List<Guid>? groups, [FromForm] Guid? defaultGroupId, [FromForm] List<Guid>? roles, [FromForm] Guid? defaultRoleId)
    {
        var user = this.ToUser(id, userName ?? string.Empty, email, phoneNumber, description, visible, enabler, dpNames, properties, groups, defaultGroupId, roles, defaultRoleId);
        try
        {
            await this.AccountManager!.UpdateUserAsync(user);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        this.HttpContext.Publish(ServiceEvents.MemberUpdated, user.Id.ToString(), new Dictionary<string, string?> { ["change"] = "admin" });
        return this.Complete(new { key = user.Id, text = this.Request.DisplayName(user.DpNames, user.UserName) }, WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 사용자를 삭제합니다. 시스템 사용자는 거부되며, 소속 그룹/역할 연결도 함께 삭제되고 담당자로 지정된 그룹은 담당자가 해제됩니다.
    /// </summary>
    /// <param name="id">삭제할 사용자 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("user/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            await this.AccountManager!.DeleteUserAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        this.HttpContext.Publish(ServiceEvents.MemberDeleted, id.ToString(), new Dictionary<string, string?> { ["by"] = "admin" });
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 사용자의 화면 표시 여부만 전환합니다.
    /// </summary>
    /// <param name="id">사용자 식별키</param>
    /// <param name="visible">전환할 표시 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("user/{id:guid}/visible")]
    public async Task<IActionResult> SetUserVisible(Guid id, [FromForm] bool visible)
    {
        try
        {
            await this.AccountManager!.SetUserVisibleAsync(id, visible);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete();
    }

    /// <summary>
    /// 사용자의 계정 활성화 여부만 전환합니다. 시스템 사용자는 비활성화할 수 없습니다.
    /// </summary>
    /// <param name="id">사용자 식별키</param>
    /// <param name="enabler">전환할 활성화 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("user/{id:guid}/enabler")]
    public async Task<IActionResult> SetUserEnabler(Guid id, [FromForm] bool enabler)
    {
        try
        {
            await this.AccountManager!.SetUserEnablerAsync(id, enabler);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete();
    }

    /// <summary>
    /// 사용자의 설명만 바꿉니다 (신고 관리의 사용자 정보 창 — 관리자·컨텐츠 관리자만).
    /// </summary>
    /// <param name="id">사용자 식별키</param>
    /// <param name="description">설명 (비우면 지움)</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("user/{id:guid}/description")]
    public async Task<IActionResult> SetUserDescription(Guid id, [FromForm] string? description)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        try
        {
            await this.AccountManager!.SetUserDescriptionAsync(id, description);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error(ex.Message);
        }
        return this.Complete();
    }

    #endregion

    #region Join Terms

    // ponytail: 단일 프로세스 잠금, 다중 서버면 DB 동시성 토큰으로 변경
    private static readonly SemaphoreSlim termsLock = new(1, 1);

    /// <summary>
    /// 가입 약관 목록을 출력 순서대로 조회합니다. 제목(과 본문)은 다국어 출력 기본 규칙으로 선택합니다.
    /// 목록은 설정 "AccountJoinTerms"(카테고리 Account), 제목/본문은 다국어 "AccountJoinTerms@Title|Body"(카테고리 = 약관 식별키)입니다.
    /// </summary>
    /// <param name="withBody">본문 포함 여부</param>
    /// <returns>items(key/text/essential, withBody 면 body 추가)</returns>
    [HttpGet("terms/list")]
    public async Task<IActionResult> GetTermsList([FromQuery] bool withBody = false)
    {
        var list = await this.Config!.GetJoinTermsAsync();
        var keys = list.Select(this.TermsKeyOf).ToArray();

        const string cacheKey = "account:terms:languages";
        var langs = await App.Cache.GetAsync<List<LanguageData>>(cacheKey);
        if (langs is null)
        {
            langs = await this.DbContext!.Languages.AsNoTracking()
                .Where(p => (p.Key == TermsTitleKey || p.Key == TermsBodyKey) && keys.Contains(p.Category))
                .ToListAsync();
            await App.Cache.SetAsync(cacheKey, langs, TimeSpan.FromMinutes(10));
        }

        string? Pick(string key, string langKey)
        {
            var matched = langs.Where(t => t.Category == key && t.Key == langKey).ToList();
            return matched.Count > 0 ? this.Request.PickCulture(matched, t => t.Culture, t => t.Value) : null;
        }

        return this.Complete(list.Select(p =>
        {
            var key = this.TermsKeyOf(p);
            return new
            {
                key,
                text = Pick(key, TermsTitleKey) ?? key,
                essential = this.TermsEssentialOf(p),
                body = withBody ? Pick(key, TermsBodyKey) ?? string.Empty : null
            };
        }));
    }

    /// <summary>
    /// 약관 편집 정보(필수 여부, 문화권별 제목/본문)를 조회합니다.
    /// </summary>
    /// <param name="key">약관 식별키</param>
    /// <returns>key/essential/titles/bodies</returns>
    [HttpGet("terms/{key}")]
    public async Task<IActionResult> GetTerms(string key)
    {
        var item = (await this.Config!.GetJoinTermsAsync()).FirstOrDefault(p => this.TermsKeyOf(p) == key);
        if (item is null) return this.NotFoundResult(WebResources.NotFoundData);

        var langs = await this.LoadLanguagesAsync(key, TermsTitleKey, TermsBodyKey);
        return this.Complete(new
        {
            key,
            essential = this.TermsEssentialOf(item),
            titles = langs[TermsTitleKey],
            bodies = langs[TermsBodyKey]
        });
    }

    /// <summary>
    /// 새 약관을 목록 끝에 추가합니다.
    /// </summary>
    /// <param name="key">약관 식별키 (생성 후 변경 불가)</param>
    /// <param name="essential">필수 동의 여부</param>
    /// <param name="titles">문화권별 제목 JSON ({문화권: 제목})</param>
    /// <param name="bodies">문화권별 본문 JSON ({문화권: 본문}, 에디터 HTML 은 정제해 저장)</param>
    /// <param name="draftKey">작성 중 에디터 이미지를 올린 임시 소유자 키 (저장 시 약관 키로 이전)</param>
    /// <returns>추가된 약관 key</returns>
    [HttpPost("terms")]
    [Valid("TermsSave")]
    public async Task<IActionResult> CreateTerms([FromForm] string? key, [FromForm] bool essential, [FromForm] string? titles, [FromForm] string? bodies, [FromForm] Guid? draftKey)
    {
        key = key?.Trim() ?? string.Empty;
        var (bodyJson, bodyHtml) = SanitizeBodies(bodies);
        await termsLock.WaitAsync();
        try
        {
            var list = await this.Config!.GetJoinTermsAsync();
            if (list.Any(p => this.TermsKeyOf(p) == key))
                return this.Error(WebResources.ValidAlreadyItem.Formats(this.Request.GetLanguage("TermsKey") ?? "Terms Key")!);

            list.Add(new JsonObject { ["key"] = key, ["essential"] = essential });
            await this.SaveJoinTermsAsync(list, key, titles, bodyJson);
        }
        finally { termsLock.Release(); }

        // 작성 중 임시 키로 올린 에디터 이미지를 약관 키로 옮긴 뒤 본문 기준으로 등록/삭제
        if (draftKey is { } draft && draft != Guid.Empty)
            await EditorFile.MoveOwnerAsync(this.DbContext!, TermsOwnerType, draft.ToString(), key);
        await EditorFile.SyncAsync(this.DbContext!, this.Config!.UploadFolder, TermsOwnerType, key, bodyHtml, false);
        return this.Complete(new { key }, WebResources.AddedComplete);
    }

    /// <summary>
    /// 약관의 필수 여부와 문화권별 제목/본문을 수정합니다. 식별키는 변경되지 않습니다.
    /// </summary>
    /// <param name="key">수정할 약관 식별키</param>
    /// <param name="essential">필수 동의 여부</param>
    /// <param name="titles">문화권별 제목 JSON ({문화권: 제목})</param>
    /// <param name="bodies">문화권별 본문 JSON ({문화권: 본문})</param>
    /// <returns>수정된 약관 key</returns>
    [HttpPut("terms/{key}")]
    [Valid("TermsSave")]
    public async Task<IActionResult> UpdateTerms(string key, [FromForm] bool essential, [FromForm] string? titles, [FromForm] string? bodies)
    {
        var (bodyJson, bodyHtml) = SanitizeBodies(bodies);
        await termsLock.WaitAsync();
        try
        {
            var list = await this.Config!.GetJoinTermsAsync();
            var item = list.FirstOrDefault(p => this.TermsKeyOf(p) == key);
            if (item is null) return this.NotFoundResult(WebResources.NotFoundData);

            item["essential"] = essential;
            await this.SaveJoinTermsAsync(list, key, titles, bodyJson);
        }
        finally { termsLock.Release(); }
        await EditorFile.SyncAsync(this.DbContext!, this.Config!.UploadFolder, TermsOwnerType, key, bodyHtml, false);
        return this.Complete(new { key }, WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 약관과 그 문화권별 제목/본문 다국어를 삭제합니다.
    /// </summary>
    /// <param name="key">삭제할 약관 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("terms/{key}")]
    public async Task<IActionResult> DeleteTerms(string key)
    {
        await termsLock.WaitAsync();
        try
        {
            var list = await this.Config!.GetJoinTermsAsync();
            var item = list.FirstOrDefault(p => this.TermsKeyOf(p) == key);
            if (item is null) return this.NotFoundResult(WebResources.NotFoundData);

            list.Remove(item);
            await this.SaveJoinTermsAsync(list, key, null, null);
        }
        finally { termsLock.Release(); }
        await EditorFile.DeleteOwnerAsync(this.DbContext!, this.Config!.UploadFolder, TermsOwnerType, key);
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 약관 출력 순서를 저장합니다. 요청에 없는 약관은 기존 순서대로 뒤에 붙습니다.
    /// </summary>
    /// <param name="keys">새 순서의 약관 식별키 목록 (반복 필드)</param>
    /// <returns>처리 결과</returns>
    [HttpPut("terms/order")]
    public async Task<IActionResult> SetTermsOrder([FromForm] List<string>? keys)
    {
        await termsLock.WaitAsync();
        try
        {
            var list = await this.Config!.GetJoinTermsAsync();
            var order = keys ?? [];
            var sorted = list.OrderBy(p => order.IndexOf(this.TermsKeyOf(p)) is var i and >= 0 ? i : int.MaxValue).ToList();
            await this.SaveJoinTermsListAsync(sorted);
        }
        finally { termsLock.Release(); }
        return this.Complete(); // 드래그마다 알림창이 뜨지 않도록 메시지 없음
    }

    /// <summary>
    /// 약관 목록만 저장하고 설정 캐시를 제거합니다.
    /// </summary>
    /// <param name="list">저장할 약관 목록</param>
    private async Task SaveJoinTermsListAsync(List<JsonNode> list)
    {
        await this.Config!.SetJoinTermsAsync(this.DbContext!.Settings, list);
        if (this.DbContext is DbContext efDb) await efDb.SaveChangesAsync();
        await this.Config!.AccountConfig.InvalidateAsync(WebConfig.JoinTermsKey);
        await App.Cache.RemoveAsync("account:terms:languages");
    }

    /// <summary>
    /// 약관 목록과 해당 약관의 제목/본문 다국어를 한 번에 저장하고 관련 캐시를 제거합니다.
    /// 제목/본문이 null 이면(삭제) 그 약관의 다국어를 모두 지웁니다.
    /// </summary>
    /// <param name="list">저장할 약관 목록</param>
    /// <param name="key">다국어를 갱신할 약관 식별키</param>
    /// <param name="titles">문화권별 제목 JSON (null 이면 삭제)</param>
    /// <param name="bodies">문화권별 본문 JSON (null 이면 삭제)</param>
    private async Task SaveJoinTermsAsync(List<JsonNode> list, string key, string? titles, string? bodies)
    {
        await this.ReplaceLanguagesAsync(key, TermsTitleKey, titles);
        await this.ReplaceLanguagesAsync(key, TermsBodyKey, bodies);
        await this.Config!.SetJoinTermsAsync(this.DbContext!.Settings, list);
        if (this.DbContext is DbContext efDb) await efDb.SaveChangesAsync();

        await this.Config!.AccountConfig.InvalidateAsync(WebConfig.JoinTermsKey);
        await this.Request.RemoveLanguageCacheAsync(TermsTitleKey, TermsBodyKey);
        await App.Cache.RemoveAsync("account:terms:languages");
    }

    /// <summary>약관 본문 에디터 이미지 소유자 형식 (소유자 식별키 = 약관 식별키, 신규 작성 중엔 임시 GUID)</summary>
    public const string TermsOwnerType = "account.terms";

    /// <summary>
    /// 문화권별 본문 JSON 의 각 본문을 에디터 HTML 정제(<see cref="EditorFile.Sanitize"/>)합니다.
    /// </summary>
    /// <param name="bodies">문화권별 본문 JSON ({문화권: 본문})</param>
    /// <returns>Json: 정제된 본문 JSON, Html: 모든 문화권 본문을 이은 HTML (사용 파일 동기화용)</returns>
    private static (string Json, string Html) SanitizeBodies(string? bodies)
    {
        var map = ParseMap(bodies).ToDictionary(p => p.Key, p => EditorFile.Sanitize(p.Value));
        return (System.Text.Json.JsonSerializer.Serialize(map), string.Concat(map.Values));
    }

    /// <summary>약관 제목 다국어 키 (카테고리 = 약관 식별키)</summary>
    private const string TermsTitleKey = WebConfig.JoinTermsKey + "@Title";

    /// <summary>약관 본문 다국어 키 (카테고리 = 약관 식별키)</summary>
    private const string TermsBodyKey = WebConfig.JoinTermsKey + "@Body";

    /// <summary>약관 항목의 식별키</summary>
    private string TermsKeyOf(JsonNode node) => node["key"]?.GetValue<string>() ?? string.Empty;

    /// <summary>약관 항목의 필수 여부</summary>
    private bool TermsEssentialOf(JsonNode node) => node["essential"]?.GetValue<bool>() ?? false;

    #endregion

    #region Mail Template

    /// <summary>메일 템플릿 본문 에디터 이미지 소유자 형식 (소유자 식별키 = 템플릿 키)</summary>
    public const string TemplateOwnerType = "account.templet";

    /// <summary>
    /// 고정 메일 템플릿 목록 (키, 이름 리소스 키, 설명 리소스 키). 항목 추가는 여기에만 하면 됩니다.
    /// 문화권별 메일 제목/본문은 다국어 "{템플릿 키}@Title|Body"(카테고리 Account)로 저장되어 GetLanguage("AccountPwChange@Title") 처럼 바로 조회됩니다.
    /// </summary>
    public static readonly (string Key, string Name, string Detail)[] TemplateItems =
    [
        ("AccountFindId", "TemplateFindIdMail", "TemplateFindIdMailDetail"),
        ("AccountPwChange", "TemplateRepwMail", "TemplateRepwMailDetail"),
        ("AccountMailConfirm", "TemplateMailVerf", "TemplateMailVerfDetail")
    ];

    /// <summary>
    /// 템플릿의 문화권별 메일 제목/본문을 조회합니다.
    /// </summary>
    /// <param name="key">템플릿 키</param>
    /// <returns>key/titles/bodies</returns>
    [HttpGet("template/{key}")]
    public async Task<IActionResult> GetTemplate(string key)
    {
        if (!this.IsTemplate(key)) return this.NotFoundResult(WebResources.NotFoundData);

        var langs = await this.LoadLanguagesAsync(WebConfig.AccountCategory, this.TemplateTitleKey(key), this.TemplateBodyKey(key));
        return this.Complete(new { key, titles = langs[this.TemplateTitleKey(key)], bodies = langs[this.TemplateBodyKey(key)] });
    }

    /// <summary>
    /// 템플릿의 문화권별 메일 제목/본문을 저장합니다. 비운 문화권은 삭제(초기화)됩니다.
    /// 본문(에디터 HTML)은 정제해 저장하고, 본문에서 쓰지 않는 에디터 이미지(소유자 account.templet/템플릿 키)는 삭제합니다.
    /// </summary>
    /// <param name="key">템플릿 키</param>
    /// <param name="titles">문화권별 제목 JSON ({문화권: 제목})</param>
    /// <param name="bodies">문화권별 본문 JSON ({문화권: 본문})</param>
    /// <returns>처리 결과</returns>
    [HttpPut("template/{key}")]
    public async Task<IActionResult> SaveTemplate(string key, [FromForm] string? titles, [FromForm] string? bodies)
    {
        if (!this.IsTemplate(key)) return this.NotFoundResult(WebResources.NotFoundData);

        var (bodyJson, bodyHtml) = SanitizeBodies(bodies);

        await this.ReplaceLanguagesAsync(WebConfig.AccountCategory, this.TemplateTitleKey(key), titles);
        await this.ReplaceLanguagesAsync(WebConfig.AccountCategory, this.TemplateBodyKey(key), bodyJson);
        if (this.DbContext! is DbContext efDb) await efDb.SaveChangesAsync();
        await EditorFile.SyncAsync(this.DbContext!, this.Config!.UploadFolder, TemplateOwnerType, key, bodyHtml, false);
        await this.Request.RemoveLanguageCacheAsync(this.TemplateTitleKey(key), this.TemplateBodyKey(key));
        await App.Cache.RemoveAsync($"template:mail:{key}");
        return this.Complete(new { key }, WebResources.SavedComplate);
    }

    /// <summary>고정 템플릿 키 여부</summary>
    private bool IsTemplate(string key) => TemplateItems.Any(p => p.Key == key);

    /// <summary>템플릿 메일 제목 다국어 키</summary>
    private string TemplateTitleKey(string key) => $"{key}@Title";

    /// <summary>템플릿 메일 본문 다국어 키</summary>
    private string TemplateBodyKey(string key) => $"{key}@Body";

    #endregion

    #region Account Rule

    // 계정 구성: AccountRule 은 설정 "AccountRule"(카테고리 AppConfig, WebConfig.AccountRule 이 읽는 위치)에,
    // 비밀번호/로그인 잠금 규칙은 설정 "Identity"(WebConfig.IdentityOptions, ConfigureIdentityOptions 가 읽는 위치)에 JSON 으로 저장한다.
    // DB 값이 없으면 현재 적용 중인 값(appsettings 또는 기본값)을 바탕으로 새로 저장하며, 저장 후 캐시 제거로 즉시 반영된다.

    /// <summary>계정 규칙 설정 키</summary>
    public const string AccountRuleKey = "AccountRule";

    /// <summary>Identity 옵션 설정 키</summary>
    public const string IdentityKey = "Identity";

    /// <summary>
    /// 현재 적용 중인 계정 규칙과 비밀번호/로그인 잠금 규칙을 조회합니다.
    /// </summary>
    /// <returns>rule(AccountRule), groups/roles(Dictionary&lt;Guid, string&gt;), defaultGroupId/defaultRoleId(Guid?), password(PasswordOptions), lockout(maxFailedAccessAttempts/lockoutMinutes/allowedForNewUsers)</returns>
    [HttpGet("rule")]
    public async Task<IActionResult> GetAccountRule()
    {
        var identity = this.CurrentIdentity;
        var rule = this.Config!.AccountRule;
        var groups = await this.LoadGroupMapAsync(rule.DefaultGroups);
        var roles = await this.LoadRoleMapAsync(rule.DefaultRoles);
        return this.Complete(new
        {
            rule,
            groups,
            defaultGroupId = rule.DefaultGroupId,
            roles,
            defaultRoleId = rule.DefaultRoleId,
            password = identity.Password,
            lockout = new
            {
                identity.Lockout.MaxFailedAccessAttempts,
                LockoutMinutes = (int)identity.Lockout.DefaultLockoutTimeSpan.TotalMinutes,
                identity.Lockout.AllowedForNewUsers
            }
        });
    }

    /// <summary>
    /// 계정 규칙과 비밀번호/로그인 잠금 규칙을 저장합니다. 전달되지 않은 항목은 현재 값을 유지합니다.
    /// </summary>
    /// <param name="defaultPassword">기본 사용자 암호 서식 ({0} = 사용자 ID)</param>
    /// <param name="uidMinSize">사용자 ID 최소 길이</param>
    /// <param name="accessExpireMin">접근 토큰 유효 기간(분)</param>
    /// <param name="accessRefreshMin">접근 토큰 만료 전 갱신 신호 시작 시간(분)</param>
    /// <param name="refreshExpireHour">갱신 토큰 만료 시간(시간)</param>
    /// <param name="accessTokenName">접근 토큰 이름</param>
    /// <param name="refreshTokenName">갱신 토큰 이름</param>
    /// <param name="refreshSignalHeaderName">갱신 신호 응답 헤더 이름</param>
    /// <param name="requiredLength">비밀번호 최소 길이</param>
    /// <param name="requiredUniqueChars">비밀번호의 서로 다른 문자 최소 수</param>
    /// <param name="requireDigit">숫자 필수</param>
    /// <param name="requireLowercase">영문 소문자 필수</param>
    /// <param name="requireUppercase">영문 대문자 필수</param>
    /// <param name="requireNonAlphanumeric">특수문자 필수</param>
    /// <param name="maxFailedAccessAttempts">잠금 전 최대 로그인 실패 횟수</param>
    /// <param name="lockoutMinutes">잠금 시간(분)</param>
    /// <param name="allowedForNewUsers">신규 사용자에게 잠금 적용</param>
    /// <param name="mailVerify">회원 가입 메일 검증 방식 (On/Warning/Off)</param>
    /// <param name="defaultGroups">가입 시 기본 소속 그룹 식별키 목록</param>
    /// <param name="defaultGroupId">가입 시 기본 대표 그룹 식별키</param>
    /// <param name="defaultRoles">가입 시 기본 부여 역할 식별키 목록</param>
    /// <param name="defaultRoleId">가입 시 기본 대표 역할 식별키</param>
    /// <param name="membersOnly">멤버 전용 구성 여부</param>
    /// <returns>저장된 계정 규칙</returns>
    [HttpPut("rule")]
    [Valid("AccountRuleSave")]
    public async Task<IActionResult> SaveAccountRule([FromForm] string? defaultPassword, [FromForm] byte? uidMinSize,
        [FromForm] int? accessExpireMin, [FromForm] int? accessRefreshMin, [FromForm] int? refreshExpireHour,
        [FromForm] string? accessTokenName, [FromForm] string? refreshTokenName, [FromForm] string? refreshSignalHeaderName,
        [FromForm] int? requiredLength, [FromForm] int? requiredUniqueChars, [FromForm] bool? requireDigit, [FromForm] bool? requireLowercase,
        [FromForm] bool? requireUppercase, [FromForm] bool? requireNonAlphanumeric,
        [FromForm] int? maxFailedAccessAttempts, [FromForm] int? lockoutMinutes, [FromForm] bool? allowedForNewUsers,
        [FromForm] MailVerifyMode? mailVerify,
        [FromForm] List<Guid>? defaultGroups, [FromForm] Guid? defaultGroupId,
        [FromForm] List<Guid>? defaultRoles, [FromForm] Guid? defaultRoleId,
        [FromForm] bool? membersOnly)
    {
        // 캐시된 인스턴스를 직접 바꾸지 않도록 새 객체에 현재 값 + 요청 값을 담는다.
        var current = this.Config!.AccountRule;
        var (groupIds, defaultGroup) = this.PickRuleDefault("defaultGroups", "defaultGroupId", defaultGroups, defaultGroupId, current.DefaultGroups, current.DefaultGroupId);
        var (roleIds, defaultRole) = this.PickRuleDefault("defaultRoles", "defaultRoleId", defaultRoles, defaultRoleId, current.DefaultRoles, current.DefaultRoleId);

        var rule = new AccountRule
        {
            DefaultPassword = defaultPassword?.Trim() ?? current.DefaultPassword,
            UidMinSize = uidMinSize ?? current.UidMinSize,
            AccessExpireMin = accessExpireMin ?? current.AccessExpireMin,
            AccessRefreshMin = accessRefreshMin ?? current.AccessRefreshMin,
            RefreshExpireHour = refreshExpireHour ?? current.RefreshExpireHour,
            AccessTokenName = accessTokenName?.Trim() ?? current.AccessTokenName,
            RefreshTokenName = refreshTokenName?.Trim() ?? current.RefreshTokenName,
            RefreshSignalHeaderName = refreshSignalHeaderName?.Trim() ?? current.RefreshSignalHeaderName,
            MailVerify = mailVerify ?? current.MailVerify,
            DefaultGroups = groupIds,
            DefaultGroupId = defaultGroup,
            DefaultRoles = roleIds,
            DefaultRoleId = defaultRole,
            MembersOnly = membersOnly.HasValue ? membersOnly.Value : current.MembersOnly
        };

        // 메일 검증을 쓰려면 시스템 설정에 저장된 SMTP 서버와 보내는 주소가 있어야 한다
        var smtp = this.Config!.AccountSmtp;
        if (rule.MailVerify != MailVerifyMode.Off && (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.From)))
            return this.Error(this.Request.GetLanguage("SmtpRequired") ?? "SMTP server and sender address are required for mail verification.");

        var identity = await this.LoadIdentityJsonAsync();
        var password = identity["password"]!.AsObject();
        var lockout = identity["lockout"]!.AsObject();
        if (requiredLength.HasValue) password["requiredLength"] = requiredLength.Value;
        if (requiredUniqueChars.HasValue) password["requiredUniqueChars"] = requiredUniqueChars.Value;
        if (requireDigit.HasValue) password["requireDigit"] = requireDigit.Value;
        if (requireLowercase.HasValue) password["requireLowercase"] = requireLowercase.Value;
        if (requireUppercase.HasValue) password["requireUppercase"] = requireUppercase.Value;
        if (requireNonAlphanumeric.HasValue) password["requireNonAlphanumeric"] = requireNonAlphanumeric.Value;
        if (maxFailedAccessAttempts.HasValue) lockout["maxFailedAccessAttempts"] = maxFailedAccessAttempts.Value;
        if (lockoutMinutes.HasValue) lockout["defaultLockoutTimeSpan"] = TimeSpan.FromMinutes(lockoutMinutes.Value).ToString("c", CultureInfo.InvariantCulture);
        if (allowedForNewUsers.HasValue) lockout["allowedForNewUsers"] = allowedForNewUsers.Value;

        await this.Config!.SetModelAsync(this.DbContext!, AccountRuleKey, rule);
        await this.Config!.SetAsync(this.DbContext!, IdentityKey, identity.ToJsonString());
        // 저장(커밋) 이후 다시 무효화해 커밋 전 읽기로 캐시된 값이 남지 않게 하고, Identity 옵션 재구성을 알린다.
        await this.Config!.InvalidateAsync(AccountRuleKey);
        await this.Config!.InvalidateAsync(IdentityKey);

        return this.Complete(rule, WebResources.SavedComplate);
    }

    /// <summary>현재 적용 중인 Identity 옵션 (DB/appsettings 반영 후 값)</summary>
    private IdentityOptions CurrentIdentity => this.HttpContext.GetService<IOptions<IdentityOptions>>()?.Value ?? new IdentityOptions();

    /// <summary>
    /// DB 의 Identity 설정 JSON 을 읽습니다. 없으면 현재 적용 중인 옵션 중 ConfigureIdentityOptions 가 반영하는 영역(password/lockout/user/signIn/stores)으로 새로 만듭니다.
    /// </summary>
    /// <returns>Identity 설정 JSON (password/lockout 객체 보장)</returns>
    private async Task<JsonObject> LoadIdentityJsonAsync()
    {
        var saved = await this.Config!.GetAsync(IdentityKey);
        if (!string.IsNullOrWhiteSpace(saved) && JsonNode.Parse(saved) is JsonObject obj)
        {
            var current = this.CurrentIdentity;
            obj["password"] ??= JsonNode.Parse(JsonConverter.ToJSON(current.Password, indented: false)!);
            obj["lockout"] ??= JsonNode.Parse(JsonConverter.ToJSON(current.Lockout, indented: false)!);
            return obj;
        }

        var identity = this.CurrentIdentity;
        var json = JsonConverter.ToJSON(new { identity.Password, identity.Lockout, identity.User, identity.SignIn, identity.Stores }, indented: false)!;
        return JsonNode.Parse(json)!.AsObject();
    }

    #endregion



    #region Helpers

    /// <summary>
    /// 트리 형제 정렬 키를 반환합니다. 정렬 등급 코드가 없으면 "zzzz"로 간주해 맨 아래로 보냅니다.
    /// </summary>
    /// <param name="group">그룹</param>
    /// <returns>정렬 키</returns>
    private string SortKey(AppGroup group) => group.Grade ?? "zzzz";

    /// <summary>
    /// 저장 결과를 화면 트리에 바로 반영할 수 있도록 트리 노드(key/text/sort/system/userCount/parentId) 형태로 변환합니다.
    /// </summary>
    /// <param name="group">저장된 그룹</param>
    /// <returns>트리 노드</returns>
    private async Task<object> ToTreeNodeAsync(AppGroup group) => new
    {
        key = group.Id,
        text = this.GroupText(group),
        sort = this.SortKey(group),
        system = group.IsSystem,
        userCount = (await this.CountGroupUsersAsync(group.Id)).GetValueOrDefault(group.Id),
        parentId = group.ParentId
    };

    /// <summary>
    /// 그룹별 소속 사용자 수(해당 그룹에 직접 속한 사용자, 탈퇴 회원 제외)를 구합니다. 사용자 목록의 그룹 필터와 같은 기준입니다.
    /// </summary>
    /// <param name="groupId">이 그룹만 집계 (없으면 전체 그룹을 한 번의 쿼리로 집계)</param>
    /// <returns>그룹 식별키별 사용자 수 (사용자가 없는 그룹은 없음)</returns>
    private async Task<Dictionary<Guid, int>> CountGroupUsersAsync(Guid? groupId = null)
    {
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var masked = AppDeletedUser.MaskPrefix.ToString();
        return await db.UserGroups.AsNoTracking()
            .Where(p => (groupId == null || p.GroupId == groupId) && !p.User!.UserName!.StartsWith(masked))
            .GroupBy(p => p.GroupId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(p => p.Key, p => p.Count);
    }

    /// <summary>
    /// 그룹 표시 텍스트를 다국어 출력 기본 규칙으로 선택하고, 표시명이 없으면 그룹명을 씁니다.
    /// </summary>
    /// <param name="group">그룹</param>
    /// <returns>표시 텍스트</returns>
    private string GroupText(AppGroup group) => this.Request.DisplayName(group.DpNames, group.Name);

    /// <summary>
    /// 그룹 식별키 목록에 대한 표시명 사전({Id: DisplayText})을 비동기로 조회합니다.
    /// </summary>
    /// <param name="ids">그룹 식별키 목록</param>
    /// <returns>그룹 식별키와 표시명 매핑 사전</returns>
    private async Task<Dictionary<Guid, string>> LoadGroupMapAsync(IEnumerable<Guid>? ids)
    {
        var list = ids?.Distinct().ToList() ?? [];
        if (list.Count == 0) return [];
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var groups = await db.Groups.AsNoTracking()
            .Include(p => p.DpNames)
            .Where(p => list.Contains(p.Id))
            .ToListAsync();
        return groups.ToDictionary(p => p.Id, p => this.Request.DisplayName(p.DpNames, p.Name));
    }

    /// <summary>
    /// 역할 식별키 목록에 대한 표시명 사전({Id: DisplayText})을 비동기로 조회합니다.
    /// </summary>
    /// <param name="ids">역할 식별키 목록</param>
    /// <returns>역할 식별키와 표시명 매핑 사전</returns>
    private async Task<Dictionary<Guid, string>> LoadRoleMapAsync(IEnumerable<Guid>? ids)
    {
        var list = ids?.Distinct().ToList() ?? [];
        if (list.Count == 0) return [];
        var db = this.HttpContext.GetService<IIdentityDbContext>()!;
        var roles = await db.Roles.AsNoTracking()
            .Include(p => p.DpNames)
            .Where(p => list.Contains(p.Id))
            .ToListAsync();
        return roles.ToDictionary(p => p.Id, this.RoleText);
    }

    /// <summary>
    /// 역할 표시 텍스트를 다국어 출력 기본 규칙으로 선택하고, 표시명이 없으면 역할명을 씁니다.
    /// </summary>
    /// <param name="role">역할</param>
    /// <returns>표시 텍스트</returns>
    private string RoleText(AppRole role) => this.Request.DisplayName(role.DpNames, role.Name);

    /// <summary>
    /// 폼 필드로 받은 그룹 값을 그룹 엔티티로 변환합니다. 표시명/속성/역할 처리 규칙은 <see cref="AddMemberData{TOwner}"/>를 따릅니다.
    /// </summary>
    /// <returns>그룹 엔티티</returns>
    private AppGroup ToGroup(Guid? id, string name, string? grade, string? description, Guid? parentId, Guid? managerId,
        bool visible, bool inherit, string? dpNames, string? properties, List<Guid>? roles, Guid? defaultRoleId)
    {
        var group = new AppGroup(name, grade, parentId: parentId, managerId: managerId, visible: visible)
        {
            Description = description,
            Inherit = inherit
        };
        if (id.HasValue) group.Id = id.Value;

        this.AddMemberData(group.DpNames, group.Properties, group.Id, dpNames, properties);
        var (roleIds, defaultRole) = this.PickDefault(roles, defaultRoleId);
        roleIds.ForEach(p => group.GroupRoles.Add(new AppGroupRole(group.Id, p, p == defaultRole)));
        return group;
    }

    /// <summary>
    /// 폼 필드로 받은 역할 값을 역할 엔티티로 변환합니다. 표시명 처리 규칙은 그룹/사용자와 같습니다(빈 값 제외).
    /// </summary>
    /// <returns>역할 엔티티</returns>
    private AppRole ToRole(Guid? id, string name, string? grade, string? description, bool visible, string? dpNames)
    {
        var role = new AppRole(name.Trim(), string.IsNullOrWhiteSpace(grade) ? null : grade.Trim(), visible) { Description = description };
        if (id.HasValue) role.Id = id.Value;
        foreach (var (culture, text) in ParseMap(dpNames))
            if (!string.IsNullOrWhiteSpace(text))
                role.DpNames.Add(new DpName<AppRole, Guid>(role.Id, culture, text));
        return role;
    }

    /// <summary>
    /// 폼 필드로 받은 사용자 값을 사용자 엔티티로 변환합니다. 소속 그룹도 역할과 같은 기본 지정 규칙을 따릅니다.
    /// </summary>
    /// <returns>사용자 엔티티</returns>
    private AppUser ToUser(Guid? id, string userName, string? email, string? phoneNumber, string? description, bool visible, bool enabler,
        string? dpNames, string? properties, List<Guid>? groups, Guid? defaultGroupId, List<Guid>? roles, Guid? defaultRoleId)
    {
        var user = new AppUser(userName.Trim(), email?.Trim() ?? string.Empty, visible, enabler)
        {
            Description = description,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim()
        };
        if (id.HasValue) user.Id = id.Value;

        this.AddMemberData(user.DpNames, user.Properties, user.Id, dpNames, properties);
        var (groupIds, defaultGroup) = this.PickDefault(groups, defaultGroupId);
        groupIds.ForEach(p => user.UserGroups.Add(new AppUserGroup(user.Id, p, p == defaultGroup, active: p == defaultGroup)));
        var (roleIds, defaultRole) = this.PickDefault(roles, defaultRoleId);
        roleIds.ForEach(p => user.UserRoles.Add(new AppUserRole(user.Id, p, p == defaultRole, active: p == defaultRole)));
        return user;
    }

    /// <summary>
    /// JSON 문자열 필드로 받은 다국어 표시명/속성을 엔티티 목록에 담습니다. 빈 표시명은 제외하고, 속성은 일반 속성(IsSystem=false)으로만 담으며
    /// "@"로 시작하는 키(시스템 속성 표식), 빈 키, 중복 키는 무시합니다. 시스템 속성 보호는 저장 로직에서 한 번 더 처리됩니다.
    /// </summary>
    /// <typeparam name="TOwner">소유자 엔티티 형식</typeparam>
    /// <param name="dpNameList">대상 표시명 목록</param>
    /// <param name="propertyList">대상 속성 목록</param>
    /// <param name="ownerId">소유자 식별키</param>
    /// <param name="dpNames">다국어 표시명 JSON ({문화권: 이름})</param>
    /// <param name="properties">속성 JSON ({키: 값})</param>
    private void AddMemberData<TOwner>(List<DpName<TOwner, Guid>> dpNameList, List<Property<TOwner, Guid>> propertyList, Guid ownerId,
        string? dpNames, string? properties)
    {
        foreach (var (culture, name) in ParseMap(dpNames))
            if (!string.IsNullOrWhiteSpace(name))
                dpNameList.Add(new DpName<TOwner, Guid>(ownerId, culture, name));

        foreach (var (key, value) in ParseMap(properties))
            if (!string.IsNullOrWhiteSpace(key) && !key.StartsWith('@') && !propertyList.Any(p => p.Key.NEqual(key.Trim())))
                propertyList.Add(new Property<TOwner, Guid>(ownerId, key.Trim(), value));
    }

    /// <summary>
    /// 연결 대상 키 목록의 중복을 제거하고 기본 항목을 정합니다. 요청 기본 키가 목록에 있으면 그 키, 없으면 첫 키입니다.
    /// </summary>
    /// <param name="ids">요청 키 목록</param>
    /// <param name="requestedDefault">요청 기본 키</param>
    /// <returns>중복 제거된 키 목록, 기본 키 (목록이 비면 Guid.Empty)</returns>
    private (List<Guid> Ids, Guid DefaultId) PickDefault(List<Guid>? ids, Guid? requestedDefault)
    {
        var distinct = (ids ?? []).Distinct().ToList();
        return (distinct, requestedDefault is Guid id && distinct.Contains(id) ? id : distinct.FirstOrDefault());
    }

    /// <summary>
    /// 계정 규칙의 가입 기본 연결(그룹/역할) 값을 정합니다. 두 폼 필드가 모두 없으면 현재 값을 유지하고,
    /// 있으면 <see cref="PickDefault"/> 규칙으로 정하되 목록이 비면 기본 키는 null 입니다.
    /// </summary>
    /// <param name="idsField">키 목록 폼 필드명</param>
    /// <param name="defaultField">기본 키 폼 필드명</param>
    /// <param name="ids">요청 키 목록</param>
    /// <param name="requestedDefault">요청 기본 키</param>
    /// <param name="currentIds">현재 키 목록</param>
    /// <param name="currentDefault">현재 기본 키</param>
    /// <returns>키 목록, 기본 키</returns>
    private (List<Guid> Ids, Guid? DefaultId) PickRuleDefault(string idsField, string defaultField, List<Guid>? ids, Guid? requestedDefault,
        List<Guid> currentIds, Guid? currentDefault)
    {
        if (!this.Request.Form.ContainsKey(idsField) && !this.Request.Form.ContainsKey(defaultField)) return (currentIds, currentDefault);
        var (picked, defaultId) = this.PickDefault(ids, requestedDefault);
        return (picked, picked.Count > 0 ? defaultId : null);
    }

    /// <summary>
    /// 편집 화면 바인딩용 속성 사전을 만듭니다. 시스템 속성은 키 앞에 "@"를 붙여 식별합니다.
    /// </summary>
    /// <typeparam name="TOwner">속성 소유자 엔티티 형식</typeparam>
    /// <param name="properties">속성 목록</param>
    /// <returns>편집용 속성 사전</returns>
    private Dictionary<string, string?> EditProperties<TOwner>(IEnumerable<Property<TOwner, Guid>> properties)
        => properties.ToDictionary(p => p.IsSystem ? $"@{p.Key}" : p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    #endregion
}
