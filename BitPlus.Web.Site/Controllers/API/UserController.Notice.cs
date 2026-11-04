using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Controllers.API;

public partial class UserController
{
    /// <summary>알림 목록 한 페이지의 알림 수</summary>
    private const int NoticePageSize = 20;

    /// <summary>
    /// 내 알림 목록을 최신순으로 한 페이지(20개)씩 돌려줍니다. 문구는 지금 화면 언어로 조합합니다(리소스 키 + 인수).
    /// </summary>
    /// <param name="page">페이지 번호 (0부터)</param>
    /// <param name="unreadOnly">true 면 확인하지 않은 알림만 (관리자 레이아웃 알림 드롭다운)</param>
    /// <returns>items([{id, type, text, url, rDate(확인 시각, 안 읽었으면 null), cDate}]), total, unread, page, pageSize</returns>
    [HttpGet("notice")]
    public async Task<IActionResult> Notices([FromQuery] int page = 0, [FromQuery] bool unreadOnly = false)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        page = Math.Max(page, 0);
        var mine = this.IdentityDb.UserNotices.AsNoTracking().Where(p => p.ReceiverId == me);
        var unread = await mine.CountAsync(p => p.RDate == null);
        var query = unreadOnly ? mine.Where(p => p.RDate == null) : mine;
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(p => p.CDate).Skip(page * NoticePageSize).Take(NoticePageSize).ToListAsync();
        var args = rows.ToDictionary(p => p.Id, p => p.ArgsOf());
        var names = await this.NoticeNamesAsync(args.Values.SelectMany(p => p));
        var items = rows.Select(p => new
        {
            id = p.Id, type = p.Type, url = p.Url, rDate = p.RDate, cDate = p.CDate,
            text = this.Lang(p.Key).Formats([.. args[p.Id].Select(arg => this.NoticeArgOf(arg, names))])
        });
        return this.Complete(new { items, total, unread, page, pageSize = NoticePageSize });
    }

    /// <summary>
    /// 알림 형식 인수의 사용자 토큰(<c>@user:식별키</c>)에 해당하는 사용자들의 지금 화면 언어 표시 이름 (다국어 출력 규칙, 없으면 사용자 ID).
    /// </summary>
    private async Task<Dictionary<Guid, string>> NoticeNamesAsync(IEnumerable<string> args)
    {
        var ids = args.Select(UserNoticeExtend.UserOf).OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0) return [];
        var users = await this.IdentityDb.Users.AsNoTracking().Include(p => p.DpNames).Where(p => ids.Contains(p.Id)).ToListAsync();
        return users.ToDictionary(p => p.Id, p => this.Request.DisplayName(p.DpNames, p.UserName));
    }

    /// <summary>알림 형식 인수 하나를 지금 화면 언어로: 익명 토큰은 "익명", 사용자 토큰은 표시 이름(없으면 "알 수 없음"), 나머지는 그대로.</summary>
    private string NoticeArgOf(string arg, Dictionary<Guid, string> names) => arg == UserNoticeExtend.AnonymousToken
        ? this.Lang("Anonymous")
        : UserNoticeExtend.UserOf(arg) is { } id ? names.GetValueOrDefault(id) ?? this.Lang("Unknown") : arg;

    /// <summary>
    /// 안 읽은 내 알림 수 (레이아웃 알림 표시용, 비로그인이면 0).
    /// </summary>
    /// <returns>안 읽은 알림 수</returns>
    [HttpGet("notice/count")]
    public async Task<IActionResult> NoticeCount()
        => this.Complete(this.CurrentUserId is { } me ? await this.IdentityDb.UserNotices.CountAsync(p => p.ReceiverId == me && p.RDate == null) : 0);

    /// <summary>
    /// 알림을 확인(읽음)으로 표시합니다. 식별키가 없으면 내 알림 전부를 확인합니다.
    /// </summary>
    /// <param name="id">알림 식별키 (없으면 전부)</param>
    /// <returns>처리 결과</returns>
    [HttpPost("notice/read/{id:guid?}")]
    public async Task<IActionResult> ReadNotice(Guid? id)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var now = DateTime.UtcNow;
        await this.IdentityDb.UserNotices.Where(p => p.ReceiverId == me && p.RDate == null && (id == null || p.Id == id)).ExecuteUpdateAsync(p => p.SetProperty(x => x.RDate, now));
        return this.Complete();
    }

    /// <summary>
    /// 알림을 지웁니다. 식별키가 없으면 확인한 내 알림 전부를 지웁니다.
    /// </summary>
    /// <param name="id">알림 식별키 (없으면 확인한 알림 전부)</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("notice/{id:guid?}")]
    public async Task<IActionResult> DeleteNotice(Guid? id)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        await this.IdentityDb.UserNotices.Where(p => p.ReceiverId == me && (id == null ? p.RDate != null : p.Id == id)).ExecuteDeleteAsync();
        return this.Complete();
    }
}
