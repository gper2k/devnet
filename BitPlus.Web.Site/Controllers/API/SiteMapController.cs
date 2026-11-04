using BitPlus.Data.CommonModel;
using BitPlus.Data.SiteMap;
using BitPlus.Web.Data;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Data.Model;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "시스템 설정 > 사이트 메뉴" 화면용 Web API 컨트롤러 (api/sitemap).
/// 메뉴 트리/하위 목록 조회, 등록/수정(상위 변경 = 이동)/삭제, 형제 순서 변경, 사용 여부 전환을 제공하며 요청은 개별 폼 필드로 받습니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public partial class SiteMapController : APIControllerBase
{
    /// <summary>
    /// <see cref="SiteMapController"/> 기본 생성자.
    /// </summary>
    public SiteMapController() : base() { }

    /// <summary>
    /// 메뉴 식별키 형식 (영문, 숫자, _ - . 1~64자. 계층 경로 구분자 '/' 불가)
    /// </summary>
    [GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial Regex MenuKeyRegex();

    /// <summary>
    /// 전체 메뉴를 계층 트리 노드(key/text/sort/parentKey/child)로 조회합니다. 형제는 정렬 순서 → 식별키 순입니다.
    /// </summary>
    /// <returns>최상위 메뉴 노드 목록</returns>
    [HttpGet("menu/tree")]
    public async Task<IActionResult> GetMenuTree()
    {
        var menus = await this.DbContext!.Menus.AsNoTracking().Include(p => p.DpNames).ToListAsync();
        return this.Complete(this.MenuNodes(menus, null));
    }

    /// <summary>
    /// 지정 메뉴의 직속 하위 메뉴 목록을 정렬 순서대로 조회합니다.
    /// </summary>
    /// <param name="parentKey">상위 메뉴 식별키 (없으면 최상위 메뉴)</param>
    /// <returns>key/text/icon/type/action/enabler/childCount 목록</returns>
    [HttpGet("menu/list")]
    public async Task<IActionResult> GetMenuList([FromQuery] string? parentKey)
    {
        parentKey = string.IsNullOrWhiteSpace(parentKey) ? null : parentKey;
        var db = this.DbContext!;
        var menus = await db.Menus.AsNoTracking().Include(p => p.DpNames)
            .Where(p => p.ParentKey == parentKey)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Key)
            .ToListAsync();
        var keys = menus.Select(p => p.Key).ToList();
        var counts = await db.Menus.AsNoTracking()
            .Where(p => p.ParentKey != null && keys.Contains(p.ParentKey))
            .GroupBy(p => p.ParentKey!)
            .ToDictionaryAsync(p => p.Key, p => p.Count());

        return this.Complete(menus.Select(p => new
        {
            key = p.Key,
            text = this.Request.DisplayName(p.DpNames, p.Key),
            icon = p.Icon,
            type = p.Type.ToString(),
            action = p.Action,
            enabler = p.Enabler,
            visible = p.Visible,
            isSystem = p.IsSystem,
            childCount = counts.GetValueOrDefault(p.Key),
            cDate = p.CDate
        }));
    }

    /// <summary>
    /// 메뉴 단건 상세 정보를 조회합니다 (다국어 명칭, 상위 메뉴 표시명, 접근 권한 대상 포함).
    /// </summary>
    /// <param name="key">메뉴 식별키</param>
    /// <returns>메뉴 상세 정보</returns>
    [HttpGet("menu/item/{key}")]
    public async Task<IActionResult> GetMenu(string key)
    {
        var menu = await this.DbContext!.Menus.AsNoTracking()
            .Include(p => p.DpNames)
            .Include(p => p.Accesses)
            .Include(p => p.Parent!).ThenInclude(p => p.DpNames)
            .FirstOrDefaultAsync(p => p.Key == key);
        if (menu is null) return this.NotFoundResult(WebResources.NotFoundData);

        var names = await this.TargetNamesAsync(menu.Accesses.Select(p => (p.TargetType, p.TargetKey)));
        return this.Complete(new
        {
            key = menu.Key,
            parentKey = menu.ParentKey,
            parentText = menu.Parent is null ? null : this.Request.DisplayName(menu.Parent.DpNames, menu.Parent.Key),
            icon = menu.Icon,
            type = menu.Type.ToString(),
            extend = menu.Extend,
            action = menu.Action,
            enabler = menu.Enabler,
            visible = menu.Visible,
            isSystem = menu.IsSystem,
            dpNames = menu.DpNames.ToDictionary(p => p.Culture, p => p.Name),
            accesses = menu.Accesses.Select(p => new
            {
                type = p.TargetType.ToString(),
                key = p.TargetKey,
                name = names.GetValueOrDefault((p.TargetType, p.TargetKey)) ?? p.TargetKey.ToString()
            })
        });
    }

    /// <summary>
    /// 새 메뉴를 생성합니다. 정렬 순서는 같은 상위 메뉴 아래 맨 끝으로 정합니다.
    /// </summary>
    /// <param name="key">메뉴 식별키 (영문, 숫자, _ - . 1~64자)</param>
    /// <param name="parentKey">상위 메뉴 식별키 (없으면 최상위)</param>
    /// <param name="dpNames">다국어 명칭 JSON ({문화권: 명칭})</param>
    /// <param name="icon">아이콘 태그 HTML (예: &lt;i class="fi fiHome"&gt;&lt;/i&gt; 또는 인라인 svg, 정제 후 저장)</param>
    /// <param name="type">메뉴 형식 (<see cref="LinkType"/>)</param>
    /// <param name="extend">확장값 (형식이 Extend 일 때)</param>
    /// <param name="action">이동 경로 또는 실행 명령</param>
    /// <param name="enabler">사용 여부</param>
    /// <param name="visible">표시 여부 (false 이면 사이트 메뉴에서 감춤, 시스템 메뉴도 바꿀 수 있음, 전달하지 않으면 그대로 두고 새 메뉴는 표시)</param>
    /// <param name="accesses">접근 권한 대상 JSON 배열 ([{type, key}], 없으면 모두 허용)</param>
    /// <returns>트리 반영용 노드</returns>
    [HttpPost("menu")]
    public async Task<IActionResult> CreateMenu([FromForm] string? key, [FromForm] string? parentKey, [FromForm] string? dpNames, [FromForm] string? icon,
        [FromForm] string? type, [FromForm] string? extend, [FromForm] string? action, [FromForm] bool enabler, [FromForm] bool? visible, [FromForm] string? accesses)
    {
        var db = this.DbContext!;
        key = key?.Trim() ?? string.Empty;
        if (!MenuKeyRegex().IsMatch(key)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("KeyOf").Formats(this.Lang("Menu"))!)!);
        if (await db.Menus.AnyAsync(p => p.Key == key)) return this.Error(WebResources.ValidAlreadyItem.Formats(this.Lang("KeyOf").Formats(this.Lang("Menu"))!)!);

        var menu = new AppMenu { Key = key };
        var error = await this.BindMenuAsync(menu, parentKey, dpNames, icon, type, extend, action, enabler, visible, accesses);
        if (error is not null) return error;

        db.Menus.Add(menu);
        await ((DbContext)db).SaveChangesAsync();
        return this.Complete(MenuNode(menu, this.Request.DisplayName(menu.DpNames, menu.Key)), WebResources.AddedComplete);
    }

    /// <summary>
    /// 메뉴를 수정합니다. 식별키는 바꿀 수 없고, 상위 메뉴가 바뀌면 새 상위 아래 맨 끝으로 이동하며 하위 메뉴 경로도 함께 갱신합니다.
    /// </summary>
    /// <param name="key">메뉴 식별키</param>
    /// <param name="parentKey">상위 메뉴 식별키 (없으면 최상위)</param>
    /// <param name="dpNames">다국어 명칭 JSON ({문화권: 명칭})</param>
    /// <param name="icon">아이콘 태그 HTML (예: &lt;i class="fi fiHome"&gt;&lt;/i&gt; 또는 인라인 svg, 정제 후 저장)</param>
    /// <param name="type">메뉴 형식 (<see cref="LinkType"/>)</param>
    /// <param name="extend">확장값 (형식이 Extend 일 때)</param>
    /// <param name="action">이동 경로 또는 실행 명령</param>
    /// <param name="enabler">사용 여부</param>
    /// <param name="visible">표시 여부 (false 이면 사이트 메뉴에서 감춤, 시스템 메뉴도 바꿀 수 있음, 전달하지 않으면 그대로 두고 새 메뉴는 표시)</param>
    /// <param name="accesses">접근 권한 대상 JSON 배열 ([{type, key}], 없으면 모두 허용)</param>
    /// <returns>트리 반영용 노드</returns>
    [HttpPut("menu/item/{key}")]
    public async Task<IActionResult> UpdateMenu(string key, [FromForm] string? parentKey, [FromForm] string? dpNames, [FromForm] string? icon,
        [FromForm] string? type, [FromForm] string? extend, [FromForm] string? action, [FromForm] bool enabler, [FromForm] bool? visible, [FromForm] string? accesses)
    {
        var db = this.DbContext!;
        var menu = await db.Menus.Include(p => p.DpNames).Include(p => p.Accesses).FirstOrDefaultAsync(p => p.Key == key);
        if (menu is null) return this.NotFoundResult(WebResources.NotFoundData);

        var error = await this.BindMenuAsync(menu, parentKey, dpNames, icon, type, extend, action, enabler, visible, accesses);
        if (error is not null) return error;

        menu.UDate = DateTime.UtcNow;
        await ((DbContext)db).SaveChangesAsync();
        return this.Complete(MenuNode(menu, this.Request.DisplayName(menu.DpNames, menu.Key)), WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 메뉴의 사용 여부만 바꿉니다 (하위 목록의 사용 토글). 시스템 메뉴는 바꿀 수 없습니다.
    /// </summary>
    /// <param name="key">메뉴 식별키</param>
    /// <param name="enabler">전환할 사용 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("menu/item/{key}/enabler")]
    public async Task<IActionResult> SetMenuEnabler(string key, [FromForm] bool enabler)
    {
        var menu = await this.DbContext!.Menus.FirstOrDefaultAsync(p => p.Key == key);
        if (menu is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (menu.IsSystem) return this.Error(this.Lang("SystemChangeWarning"));

        menu.Enabler = enabler;
        menu.UDate = DateTime.UtcNow;
        await ((DbContext)this.DbContext!).SaveChangesAsync();
        return this.Complete();
    }

    /// <summary>
    /// 같은 상위 메뉴 아래 형제 메뉴의 순서를 전달 순서대로 저장합니다 (하위 목록 드래그 정렬).
    /// </summary>
    /// <param name="parentKey">상위 메뉴 식별키 (없으면 최상위)</param>
    /// <param name="keys">형제 메뉴 식별키 목록 (반복 필드, 전달 순서 = 정렬 순서)</param>
    /// <returns>처리 결과</returns>
    [HttpPut("menu/order")]
    public async Task<IActionResult> SetMenuOrder([FromForm] string? parentKey, [FromForm] List<string>? keys)
    {
        parentKey = string.IsNullOrWhiteSpace(parentKey) ? null : parentKey;
        var order = keys ?? [];
        var siblings = await this.DbContext!.Menus.Where(p => p.ParentKey == parentKey).ToListAsync();
        foreach (var menu in siblings)
        {
            var index = order.IndexOf(menu.Key);
            if (index >= 0) menu.SortOrder = index + 1;
        }
        await ((DbContext)this.DbContext!).SaveChangesAsync();
        return this.Complete();
    }

    /// <summary>
    /// 메뉴를 하위 메뉴까지 함께 삭제합니다. 다국어 명칭과 접근 권한은 메뉴와 함께 삭제됩니다.
    /// 시스템 메뉴이거나 하위에 시스템 메뉴가 있으면 삭제할 수 없습니다.
    /// </summary>
    /// <param name="key">삭제할 메뉴 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("menu/item/{key}")]
    public async Task<IActionResult> DeleteMenu(string key)
    {
        var db = this.DbContext!;
        var menu = await db.Menus.FirstOrDefaultAsync(p => p.Key == key);
        if (menu is null) return this.NotFoundResult(WebResources.NotFoundData);

        var selfToken = MenuTree.SelfToken(key); // 자손 = Location(조상 경로)에 자신의 키가 들어 있는 메뉴
        var targets = await db.Menus.Where(p => p.Key == key || (p.Location != null && p.Location.Contains(selfToken))).ToListAsync();
        if (targets.Any(p => p.IsSystem)) return this.Error(WebResources.SystemDelWarning);
        db.Menus.RemoveRange(targets);
        await ((DbContext)db).SaveChangesAsync();
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 요청 값을 검증해 메뉴에 반영합니다(상위 변경 시 순환 확인, 정렬 순서/경로 갱신 포함, 표시 여부는 시스템 메뉴도 반영). 검증 실패 시 오류 응답을, 성공 시 null 을 반환합니다.
    /// </summary>
    private async Task<IActionResult?> BindMenuAsync(AppMenu menu, string? parentKey, string? dpNames, string? icon,
        string? type, string? extend, string? action, bool enabler, bool? visible, string? accesses)
    {
        var db = this.DbContext!;
        var names = ParseMap(dpNames)
            .Where(p => !string.IsNullOrWhiteSpace(p.Key) && !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => p.Key.Trim(), p => p.Value!.Trim(), StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("DpName"))!);

        if (!Enum.TryParse<LinkType>(type, true, out var linkType) || !Enum.IsDefined(linkType))
            return this.Error(WebResources.ValidRegex.Formats(this.Lang("LinkType"))!);

        // 아이콘은 태그째 받아 출력하므로 아이콘 태그(i/span/svg 도형)만 남기고 정제한다
        var iconHtml = IconHtml.Sanitize(icon);

        parentKey = string.IsNullOrWhiteSpace(parentKey) ? null : parentKey.Trim();
        var isNew = string.IsNullOrEmpty(menu.Location);
        if (isNew || parentKey != menu.ParentKey)
        {
            var parent = parentKey is null ? null : await db.Menus.FirstOrDefaultAsync(p => p.Key == parentKey);
            if (parentKey is not null && parent is null) return this.NotFoundResult(WebResources.NotFoundTarget);
            if (!isNew && !MenuTree.CanMove(menu, parent)) return this.Error(WebResources.CircularRefrenceMessage); // 자신 또는 자신의 자손 아래로는 옮길 수 없다
            // 컨텐츠맵 이하 메뉴(게시판 메뉴 등)는 컨텐츠맵 또는 그 하위 메뉴 아래로만 옮길 수 있다
            if (!isNew && (menu.Location?.Contains(MenuTree.SelfToken(DefaultContentHandler.BoardMenuRootKey), StringComparison.OrdinalIgnoreCase) ?? false)
                && !(parent is not null && (parent.Key == DefaultContentHandler.BoardMenuRootKey || (parent.Location?.Contains(MenuTree.SelfToken(DefaultContentHandler.BoardMenuRootKey), StringComparison.OrdinalIgnoreCase) ?? false))))
                return this.Error(this.Lang("MenuContentMapOnly"));
            await MenuTree.MoveAsync(db, menu, parent, isNew);
        }

        menu.Icon = iconHtml;
        if (visible.HasValue) menu.Visible = visible.Value; // 감춤은 시스템 메뉴도 바꿀 수 있다
        // 시스템 메뉴는 이동 형식·경로·사용 여부·접근 권한을 바꿀 수 없다 (요청 값은 무시, 표시명과 아이콘만 반영)
        var locked = !isNew && menu.IsSystem;
        if (!locked)
        {
            menu.Type = linkType;
            menu.Extend = linkType == LinkType.Extend ? extend.TrimOrNull() : null;
            menu.Action = action.TrimOrNull();
            menu.Enabler = enabler;
        }

        // 자식 목록 병합 — 빠진 항목은 삭제, 남은 항목은 값만 갱신, 새 항목은 추가
        menu.DpNames.RemoveAll(p => !names.ContainsKey(p.Culture));
        foreach (var (culture, text) in names)
        {
            var current = menu.DpNames.FirstOrDefault(p => p.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));
            if (current is null) menu.DpNames.Add(new DpName<AppMenu, string>(menu.Key, culture, text));
            else current.Name = text;
        }

        if (locked) return null;
        var targets = ParseTargets(accesses);
        menu.Accesses.RemoveAll(p => !targets.Contains((p.TargetType, p.TargetKey)));
        foreach (var (targetType, targetKey) in targets)
            if (!menu.Accesses.Any(p => p.TargetType == targetType && p.TargetKey == targetKey))
                menu.Accesses.Add(new AppMenuAccess(menu.Key, targetType, targetKey));
        return null;
    }

    /// <summary>
    /// 지정 상위 메뉴 아래 메뉴를 재귀적으로 트리 노드로 만듭니다.
    /// </summary>
    private List<object> MenuNodes(List<AppMenu> menus, string? parentKey) =>
        [.. menus.Where(p => p.ParentKey == parentKey).OrderBy(p => p.SortOrder).ThenBy(p => p.Key)
            .Select(p => MenuNode(p, this.Request.DisplayName(p.DpNames, p.Key), this.MenuNodes(menus, p.Key)))];

    /// <summary>
    /// 메뉴 하나를 트리 노드(key/text/sort/parentKey/disabled/child)로 만듭니다. sort 는 정렬 순서를 자릿수 맞춘 문자열입니다.
    /// </summary>
    private static object MenuNode(AppMenu menu, string text, List<object>? child = null) => new
    {
        key = menu.Key,
        text,
        sort = menu.SortOrder.ToString("D6"),
        parentKey = menu.ParentKey,
        system = menu.IsSystem,
        child
    };
}
