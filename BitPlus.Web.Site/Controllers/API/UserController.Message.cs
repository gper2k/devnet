using BitPlus.Data.Account.Model;
using BitPlus.Data.CommonModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class UserController
{
    /// <summary>대화 한 번에 가져오는 메시지 수 (처음엔 최근 것, 위로 스크롤하면 그 이전 것)</summary>
    private const int ConversationSize = 30;

    /// <summary>대화 상대 목록 한 페이지의 상대 수</summary>
    private const int ThreadPageSize = 10;

    /// <summary>
    /// 사용자에게 메시지를 보냅니다 (자신에게는 못 보냄, 활성 사용자만, 받는 사람이 메시지 받기를 끈 경우 거부, 1~1000자, 연속 발송은 5초 간격).
    /// </summary>
    /// <param name="userId">받는 사용자 식별키</param>
    /// <param name="message">메시지 (글자 그대로 저장, 화면에서 이스케이프)</param>
    /// <returns>처리 결과</returns>
    [HttpPost("{userId:guid}/message")]
    public async Task<IActionResult> SendMessage(Guid userId, [FromForm] string? message)
    {
        if (this.CurrentUserId is not { } senderId) return this.UnauthorizedResult();
        message = message?.Trim();
        if (string.IsNullOrEmpty(message) || message.Length > MaxMessage) return this.Error(WebResources.ValidRequired.Formats(this.Lang("ContentBody"))!);
        if (userId == senderId) return this.Error(this.Lang("MessageSelf"));

        var db = (DbContext)this.DbContext!;
        if (!await db.Set<AppUser>().AnyAsync(p => p.Id == userId && p.Enabler)) return this.NotFoundResult();
        if (await db.Set<Property<AppUser, Guid>>().AnyAsync(p => p.OwnerID == userId && p.Key == MessageOffKey && p.Value == "true")) return this.Error(this.Lang("MessageRefused"));
        var since = DateTime.UtcNow - MessageGap;
        if (await db.Set<AppUserMessage>().AnyAsync(p => p.SenderId == senderId && p.CDate > since)) return this.Error(this.Lang("MailResendWait"));

        db.Set<AppUserMessage>().Add(new AppUserMessage(userId, senderId, message));
        await db.SaveChangesAsync();
        return this.Complete(message: this.Lang("MessageSent"));
    }

    /// <summary>
    /// 내 메시지함의 대화 상대 목록: 주고받은 메시지(내가 지운 것 제외)를 상대별로 묶어 마지막 메시지가 최근인 순서로, 한 페이지(10명)씩 돌려줍니다.
    /// </summary>
    /// <param name="page">페이지 번호 (0부터)</param>
    /// <returns>items([{userId, name, photo, group(기본 그룹), role(기본 역할), count(메시지 수), unread(안 읽은 받은 메시지 수), last(마지막 메시지 시각)}]), total(상대 수), page, pageSize</returns>
    [HttpGet("message/threads")]
    public async Task<IActionResult> MessageThreads([FromQuery] int page = 0)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        page = Math.Max(page, 0);
        var sent = db.Set<AppUserMessage>().AsNoTracking()
            .Where(p => p.SenderId == me && !p.SenderDeleted)
            .Select(p => new { Other = p.ReceiverId, Unread = false, p.CDate });
        var received = db.Set<AppUserMessage>().AsNoTracking()
            .Where(p => p.ReceiverId == me && !p.ReceiverDeleted)
            .Select(p => new { Other = p.SenderId, Unread = p.RDate == null, p.CDate });
        var grouped = sent.Concat(received)
            .GroupBy(p => p.Other)
            .Select(g => new { Other = g.Key, Count = g.Count(), Unread = g.Count(x => x.Unread), Last = g.Max(x => x.CDate) });
        var total = await grouped.CountAsync();
        var threads = await grouped.OrderByDescending(p => p.Last).ThenBy(p => p.Other).Skip(page * ThreadPageSize).Take(ThreadPageSize).ToListAsync();
        var ids = threads.Select(p => p.Other).ToList();
        var users = await db.Set<AppUser>().AsNoTracking().AsSplitQuery().Include(p => p.DpNames).Include(p => p.Properties.Where(x => x.Key == PhotoKey))
            .Include(p => p.UserGroups).ThenInclude(p => p.Group!.DpNames).Include(p => p.UserRoles).ThenInclude(p => p.Role!.DpNames)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var items = threads.Select(p =>
        {
            var user = users.GetValueOrDefault(p.Other);
            var group = user?.UserGroups.FirstOrDefault(x => x.Default) ?? user?.UserGroups.FirstOrDefault(); // 기본 그룹·역할 (없으면 첫 번째)
            var role = user?.UserRoles.FirstOrDefault(x => x.Default) ?? user?.UserRoles.FirstOrDefault();
            return new
            {
                userId = p.Other,
                name = user is null ? this.Lang("Unknown") : this.Request.DisplayName(user.DpNames, user.UserName),
                photo = user?.Properties.FirstOrDefault()?.Value,
                group = group is null ? null : this.Request.DisplayName(group.Group?.DpNames, group.Group?.Name),
                role = role is null ? null : this.Request.DisplayName(role.Role?.DpNames, role.Role?.Name),
                count = p.Count,
                unread = p.Unread,
                last = p.Last
            };
        });
        return this.Complete(new { items, total, page, pageSize = ThreadPageSize });
    }

    /// <summary>
    /// 상대와 주고받은 메시지(내가 지운 것 제외) 최근 30개를 오래된 순으로 돌려주고, 그 상대에게서 받은 안 읽은 메시지를 읽음으로 표시합니다.
    /// before 를 주면 그 메시지보다 이전 메시지 30개 (위로 스크롤해 더 불러오기).
    /// </summary>
    /// <param name="userId">상대 사용자 식별키</param>
    /// <param name="before">이 메시지(식별키)보다 이전 메시지만 (없으면 최근부터)</param>
    /// <returns>items([{id, mine(내가 보낸 것), message, cDate, rDate(상대가 읽은 시각, 안 읽었으면 null — 내가 보낸 것만 의미)}]), more(더 이전 메시지가 있는지)</returns>
    [HttpGet("message/{userId:guid}")]
    public async Task<IActionResult> Conversation(Guid userId, [FromQuery] Guid? before)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        var query = MyMessages(db, me).Where(p => p.SenderId == userId || p.ReceiverId == userId);
        var until = before is { } beforeId ? await db.Set<AppUserMessage>().Where(p => p.Id == beforeId).Select(p => (DateTime?)p.CDate).FirstOrDefaultAsync() : null;
        if (until is { } date) query = query.Where(p => p.CDate < date);
        var items = await query.OrderByDescending(p => p.CDate).Take(ConversationSize + 1)
            .Select(p => new { id = p.Id, mine = p.SenderId == me, message = p.Message, cDate = p.CDate, rDate = p.RDate }).ToListAsync();
        var now = DateTime.UtcNow;
        await db.Set<AppUserMessage>().Where(p => p.ReceiverId == me && p.SenderId == userId && p.RDate == null).ExecuteUpdateAsync(p => p.SetProperty(x => x.RDate, now));
        var more = items.Count > ConversationSize;
        if (more) items.RemoveAt(ConversationSize);
        items.Reverse();
        return this.Complete(new { items, more });
    }

    /// <summary>
    /// 메시지를 내 쪽에서 지웁니다 (보낸 사람·받은 사람이 각자 지우고, 둘 다 지운 메시지는 완전히 삭제).
    /// </summary>
    /// <param name="id">메시지 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("message/{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        var message = await db.Set<AppUserMessage>().FirstOrDefaultAsync(p => p.Id == id && (p.SenderId == me || p.ReceiverId == me));
        if (message is null) return this.NotFoundResult();

        if (message.SenderId == me) message.SenderDeleted = true;
        if (message.ReceiverId == me) message.ReceiverDeleted = true;
        if (message.SenderDeleted && message.ReceiverDeleted) db.Set<AppUserMessage>().Remove(message);
        await db.SaveChangesAsync();
        return this.Complete();
    }

    /// <summary>
    /// 안 읽은 받은 메시지를 최신순으로 최근 것부터 돌려줍니다 (관리자 레이아웃 메시지 드롭다운용, 읽음 표시는 하지 않음).
    /// </summary>
    /// <returns>items([{id, userId(보낸 사람), name, message, cDate}]), unread(안 읽은 받은 메시지 수)</returns>
    [HttpGet("message/unread")]
    public async Task<IActionResult> UnreadMessages()
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        var query = MyMessages(db, me).Where(p => p.ReceiverId == me && p.RDate == null);
        var unread = await query.CountAsync();
        var rows = await query.OrderByDescending(p => p.CDate).Take(ConversationSize).ToListAsync();
        var ids = rows.Select(p => p.SenderId).Distinct().ToList();
        var names = await db.Set<AppUser>().AsNoTracking().Include(p => p.DpNames).Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => this.Request.DisplayName(p.DpNames, p.UserName));
        var items = rows.Select(p => new { id = p.Id, userId = p.SenderId, name = names.GetValueOrDefault(p.SenderId) ?? this.Lang("Unknown"), message = p.Message, cDate = p.CDate });
        return this.Complete(new { items, unread });
    }

    /// <summary>
    /// 받은 메시지를 읽음으로 표시합니다. 식별키가 없으면 안 읽은 받은 메시지 전부를 읽음으로 표시합니다.
    /// </summary>
    /// <param name="id">메시지 식별키 (없으면 전부)</param>
    /// <returns>처리 결과</returns>
    [HttpPost("message/read/{id:guid?}")]
    public async Task<IActionResult> ReadMessage(Guid? id)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var now = DateTime.UtcNow;
        await ((DbContext)this.DbContext!).Set<AppUserMessage>().Where(p => p.ReceiverId == me && p.RDate == null && (id == null || p.Id == id)).ExecuteUpdateAsync(p => p.SetProperty(x => x.RDate, now));
        return this.Complete();
    }

    /// <summary>내가 보냈거나 받은 메시지 중 내가 지우지 않은 것</summary>
    private static IQueryable<AppUserMessage> MyMessages(DbContext db, Guid me)
        => db.Set<AppUserMessage>().AsNoTracking().Where(p => (p.SenderId == me && !p.SenderDeleted) || (p.ReceiverId == me && !p.ReceiverDeleted));
}
