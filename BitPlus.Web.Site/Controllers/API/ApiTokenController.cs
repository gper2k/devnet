using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "외부 서비스 > 접근 토큰" 화면용 Web API 컨트롤러 (api/apitoken). 외부에서 사이트 API 를 직접 호출할 접근 토큰을 발급·조회·삭제하며 관리자 정책만 쓸 수 있습니다.
/// 토큰은 수정하지 않고 삭제 후 다시 발급합니다. 원문은 발급 응답에서 한 번만 돌려주고 DB 에는 해시만 저장합니다.
/// 사용은 <c>Authorization: Bearer {토큰}</c> (api 경로만, <c>AccountAuthMiddleware</c>)이며 토큰 사용자의 권한이 허용하는 모든 API 를 호출할 수 있습니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public class ApiTokenController : APIControllerBase
{
    /// <summary>
    /// <see cref="ApiTokenController"/> 기본 생성자.
    /// </summary>
    public ApiTokenController() : base() { }

    /// <summary>계정 DB (토큰·사용자 테이블)</summary>
    private IIdentityDbContext Db => this.HttpContext.GetService<IIdentityDbContext>()!;

    /// <summary>
    /// 접근 토큰 목록을 발급 최신순으로 조회합니다 (원문·해시는 내려주지 않음).
    /// </summary>
    /// <returns>[{id, name, userId, userName, expire, lastUsed, lastIP, cDate, valid}]</returns>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var now = DateTime.UtcNow;
        var items = await this.Db.ApiTokens.AsNoTracking().OrderByDescending(p => p.CDate)
            .Select(p => new
            {
                p.Id, p.Name, p.UserId, userName = p.User!.UserName,
                p.Expire, p.LastUsed, p.LastIP, p.CDate,
                valid = p.Expire == null || p.Expire >= now
            }).ToListAsync();
        return this.Complete(items);
    }

    /// <summary>
    /// 접근 토큰을 발급합니다. 응답의 token(원문)은 이때 한 번만 내려주므로 화면에서 복사해 두어야 합니다.
    /// </summary>
    /// <param name="name">토큰 이름 (필수, 64자 이내)</param>
    /// <param name="userId">토큰으로 인증할 사용자 (비우면 발급하는 관리자 자신)</param>
    /// <param name="expireDays">유효 일수 (1~3650, 비우면 무기한)</param>
    /// <returns>{id, token}</returns>
    [HttpPost]
    public async Task<IActionResult> Issue([FromForm] string? name, [FromForm] Guid? userId, [FromForm] int? expireDays)
    {
        name = name.Clip(64);
        if (name.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Name"))!);
        if (expireDays is < 1 or > 3650) return this.Error(WebResources.ValidRegex.Formats(this.Lang("ExpireDate"))!);
        var db = this.Db;
        var targetId = userId ?? this.CurrentUserId!.Value;
        if (!await db.Users.AnyAsync(p => p.Id == targetId && p.Enabler)) return this.Error(WebResources.NotFoundTarget);

        var (token, raw) = AppApiToken.Issue(name, targetId, expireDays is null ? null : DateTime.UtcNow.AddDays(expireDays.Value), this.CurrentUserId);
        db.ApiTokens.Add(token);
        await db.SaveChangesAsync();
        return this.Complete(new { token.Id, token = raw }, this.Lang("ApiTokenIssued"));
    }

    /// <summary>
    /// 접근 토큰을 삭제합니다 (캐시도 지워 즉시 무효).
    /// </summary>
    /// <param name="id">토큰 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var db = this.Db;
        var token = await db.ApiTokens.FirstOrDefaultAsync(p => p.Id == id);
        if (token is null) return this.NotFoundResult(WebResources.NotFoundData);
        db.ApiTokens.Remove(token);
        await db.SaveChangesAsync();
        await App.Cache.RemoveAsync(AppApiToken.CacheKey(token.Hash));
        return this.Complete(null, WebResources.DeletedComplete);
    }
}
