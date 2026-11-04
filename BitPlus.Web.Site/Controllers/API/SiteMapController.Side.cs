using BitPlus.Web.Data.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class SiteMapController
{
    /// <summary>
    /// 메뉴 트리 확장 메뉴 "사이드 구성"의 준비 값: 메뉴와 그 하위 메뉴 중 사이드 구성을 적용할 게시판·페이지 뷰 수, 연결 메뉴 선택 목록(사용자 메뉴 트리).
    /// </summary>
    /// <param name="key">기준 메뉴 식별키</param>
    /// <returns>boards, pages (대상 수), sideMenus ([{key, text, depth}])</returns>
    [HttpGet("menu/side/{key}")]
    public async Task<IActionResult> GetMenuSide(string key)
    {
        var db = this.DbContext!;
        if (!await db.Menus.AnyAsync(p => p.Key == key)) return this.NotFoundResult(WebResources.NotFoundData);
        var (rules, pages) = await DefaultContentHandler.SideTargetsAsync(db, key);
        return this.Complete(new { boards = rules.Count, pages = pages.Count, sideMenus = await DefaultContentHandler.SideMenusAsync(db, this.Request) });
    }

    /// <summary>
    /// 메뉴와 그 모든 하위 메뉴의 이동 경로가 가리키는 게시판(<c>/content/{번호}</c>)·페이지 뷰(<c>/view/{번호}</c>)에 같은 사이드 구성을 한 번에 저장합니다. 그 밖의 메뉴는 건너뜁니다.
    /// </summary>
    /// <param name="key">기준 메뉴 식별키</param>
    /// <param name="sideHidden">사이드 감춤</param>
    /// <param name="sidePosition">사이드 위치 (Left/Right)</param>
    /// <param name="sideMenuKey">연결 메뉴 키 (비면 자동, 사용자 메뉴 하위가 아니면 자동으로 저장)</param>
    /// <returns>boards, pages (적용한 수)</returns>
    [HttpPut("menu/side/{key}")]
    public async Task<IActionResult> SaveMenuSide(string key, [FromForm] bool sideHidden, [FromForm] string? sidePosition, [FromForm] string? sideMenuKey)
    {
        var db = this.DbContext!;
        if (!await db.Menus.AnyAsync(p => p.Key == key)) return this.NotFoundResult(WebResources.NotFoundData);
        var right = string.Equals(sidePosition, DefaultContentHandler.SideRight, StringComparison.OrdinalIgnoreCase);
        var menuKey = await DefaultContentHandler.SideMenuKeyAsync(db, sideMenuKey);
        var (rules, pages) = await DefaultContentHandler.SideTargetsAsync(db, key);
        rules.ForEach(p => DefaultContentHandler.SetRuleSide(p, sideHidden, right, menuKey));
        pages.ForEach(p => PageViewContentHandler.SetPageSide(p, sideHidden, right, menuKey));
        await ((DbContext)db).SaveChangesAsync();
        return this.Complete(new { boards = rules.Count, pages = pages.Count }, WebResources.UpdatedComplete);
    }
}
