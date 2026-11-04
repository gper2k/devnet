using BitPlus.Web.Site.Models.Manage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ManageController
{
    #region Menus (SiteMapController 위임)

    /// <summary>
    /// 전체 사이트 메뉴를 계층형 트리 구조(key, text, sort, parentKey, children)로 조회합니다.
    /// <see cref="SiteMapController.GetMenuTree"/>에 직접 위임합니다.
    /// </summary>
    /// <returns>최상위 메뉴 노드 목록</returns>
    [HttpGet("menus/tree")]
    public Task<IActionResult> GetMenuTree() => this.SiteMap.GetMenuTree();

    /// <summary>
    /// 신규 사이트 메뉴를 생성합니다.
    /// <see cref="SiteMapController.CreateMenu"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="request">메뉴 생성 요청 모델</param>
    /// <returns>생성된 메뉴 정보</returns>
    [HttpPost("menus")]
    public Task<IActionResult> CreateMenu([FromBody] ManageMenuSaveRequest request)
    {
        var dpNamesJson = request.DpNames is { Count: > 0 }
            ? JsonSerializer.Serialize(request.DpNames)
            : !string.IsNullOrWhiteSpace(request.Name)
                ? JsonSerializer.Serialize(new Dictionary<string, string> { [this.CurrentCulture.Name] = request.Name.Trim() })
                : null;

        return this.SiteMap.CreateMenu(
            request.Key,
            request.ParentKey,
            dpNamesJson,
            request.Icon,
            request.Type,
            request.Extend,
            request.Action,
            request.Enabler,
            request.Visible,
            null);
    }

    /// <summary>
    /// 기존 사이트 메뉴 정보를 수정합니다.
    /// <see cref="SiteMapController.UpdateMenu"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="key">수정할 메뉴 식별키</param>
    /// <param name="request">메뉴 수정 요청 모델</param>
    /// <returns>수정된 메뉴 정보</returns>
    [HttpPut("menus/{key}")]
    public Task<IActionResult> UpdateMenu(string key, [FromBody] ManageMenuSaveRequest request)
    {
        var dpNamesJson = request.DpNames is { Count: > 0 }
            ? JsonSerializer.Serialize(request.DpNames)
            : !string.IsNullOrWhiteSpace(request.Name)
                ? JsonSerializer.Serialize(new Dictionary<string, string> { [this.CurrentCulture.Name] = request.Name.Trim() })
                : null;

        return this.SiteMap.UpdateMenu(
            key,
            request.ParentKey,
            dpNamesJson,
            request.Icon,
            request.Type,
            request.Extend,
            request.Action,
            request.Enabler,
            request.Visible,
            null);
    }

    /// <summary>
    /// 메뉴를 삭제합니다 (하위 자손 메뉴 포함).
    /// <see cref="SiteMapController.DeleteMenu"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="key">삭제할 메뉴 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("menus/{key}")]
    public Task<IActionResult> DeleteMenu(string key) => this.SiteMap.DeleteMenu(key);

    /// <summary>
    /// 메뉴 사용 여부(Enabler)를 On/Off 토글합니다.
    /// <see cref="SiteMapController.SetMenuEnabler"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="key">메뉴 식별키</param>
    /// <param name="enabler">사용 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("menus/{key}/enabler")]
    public Task<IActionResult> SetMenuEnabler(string key, [FromQuery] bool enabler) => this.SiteMap.SetMenuEnabler(key, enabler);

    #endregion
}
