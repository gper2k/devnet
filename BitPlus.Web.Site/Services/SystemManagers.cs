using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Web.Site.Data;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Services;

/// <summary>
/// 최종 관리자(<see cref="SystemNames.SystemManagerRole"/>) 사용자 찾기 — 서비스가 관리자에게 알리거나 메일을 보낼 때 씁니다.
/// </summary>
public static class SystemManagers
{
    /// <summary>
    /// 최종 관리자 역할을 직접 가졌거나 그 역할이 있는 그룹에 속한 활성 사용자 질의.
    /// </summary>
    /// <param name="db">계정 DB</param>
    /// <returns>사용자 질의 (추적 없음)</returns>
    public static IQueryable<AppUser> Of(IIdentityDbContext db)
    {
        var roleIds = db.Roles.Where(p => p.Name == SystemNames.SystemManagerRole).Select(p => p.Id);
        var groupIds = db.GroupRoles.Where(p => roleIds.Contains(p.RoleId)).Select(p => p.GroupId);
        var userIds = db.UserRoles.Where(p => roleIds.Contains(p.RoleId)).Select(p => p.UserId)
            .Union(db.UserGroups.Where(p => groupIds.Contains(p.GroupId)).Select(p => p.UserId));
        return db.Users.AsNoTracking().Where(p => userIds.Contains(p.Id) && p.Enabler);
    }
}
