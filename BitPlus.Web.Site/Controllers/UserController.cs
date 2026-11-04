using BitPlus.Data.Account.Model;
using BitPlus.Web.HTTP.View;
using BitPlus.Web.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserApi = BitPlus.Web.Site.Controllers.API.UserController;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 회원 페이지 MVC 뷰 컨트롤러입니다. 내 페이지는 정보(<c>/user</c>)·알림(<c>/user/notice</c>)·메시지(<c>/user/message</c>)·작성글(<c>/user/posts</c>)·환경설정(<c>/user/setting</c>) 탭이고,
/// 다른 사용자의 작성글 보기(<c>/user/{식별키}</c>, 게시판 작성자 메뉴의 "작성글 보기")도 여기서 보여 줍니다. 모두 로그인 사용자만 열 수 있습니다(비로그인 401).
/// 뷰는 <c>Views/User</c>, 데이터 처리는 <c>api/user</c>(<see cref="UserApi"/>)·<c>api/account/password</c> 입니다.
/// </summary>
public class UserController : ViewControllerBase
{
    /// <summary>
    /// <see cref="UserController"/> 기본 생성자.
    /// </summary>
    public UserController() : base() { }

    /// <summary>내 정보 페이지 (기본 정보, 정보 편집·암호 변경·이미지 등록/초기화)</summary>
    /// <returns>정보 뷰</returns>
    [HttpGet("/user")]
    public Task<IActionResult> Info() => this.MyPageAsync("info", "Index");

    /// <summary>내 알림 페이지 (시스템 알림 목록 — 확인·삭제)</summary>
    /// <returns>알림 뷰</returns>
    [HttpGet("/user/notice")]
    public Task<IActionResult> Notice() => this.MyPageAsync("notice", "Notice");

    /// <summary>내 메시지 페이지 (왼쪽 대화 상대 목록, 오른쪽 대화 — 처음에는 가장 최근 상대)</summary>
    /// <returns>메시지 뷰</returns>
    [HttpGet("/user/message")]
    public Task<IActionResult> Message() => this.MyPageAsync("message", "Message");

    /// <summary>내 작성글 페이지 (게시물·댓글 탭)</summary>
    /// <returns>작성글 뷰</returns>
    [HttpGet("/user/posts")]
    public Task<IActionResult> Posts() => this.MyPageAsync("posts", "Posts");

    /// <summary>내 환경설정 페이지 (언어·테마·메시지 받기)</summary>
    /// <returns>환경설정 뷰</returns>
    [HttpGet("/user/setting")]
    public Task<IActionResult> Setting() => this.MyPageAsync("setting", "Setting");

    /// <summary>
    /// 다른 사용자의 작성글 보기 (게시판 작성자 메뉴): 이름·이미지·가입일과 메시지 보내기, 그 사용자가 쓴 게시물·댓글 탭. 내 식별키면 내 작성글 페이지로 이동합니다.
    /// </summary>
    /// <param name="userId">사용자 식별키</param>
    /// <returns>작성글 뷰 (없거나 비활성 사용자면 404)</returns>
    [HttpGet("/user/{userId:guid}")]
    public async Task<IActionResult> Others(Guid userId)
    {
        if (!this.IsAuthenticated) return this.Unauthorized();
        if (userId == this.CurrentUserId) return this.Redirect("/user/posts");
        var model = await this.ModelOfAsync(userId, "posts", false);
        return model is null ? this.NotFound() : this.PageOf(model, "Posts");
    }

    /// <summary>내 페이지 공통: 로그인 확인 → 모델 → 뷰</summary>
    private async Task<IActionResult> MyPageAsync(string tab, string view)
    {
        if (this.CurrentUserId is not { } me) return this.Unauthorized();
        var model = await this.ModelOfAsync(me, tab, true);
        return model is null ? this.NotFound() : this.PageOf(model, view);
    }

    /// <summary>제목·페이지 키를 정하고 뷰를 돌려줍니다.</summary>
    private IActionResult PageOf(UserPageModel model, string view)
    {
        this.SetTitle(model.Name);
        this.SetPageKey("User");
        return this.View(view, model);
    }

    /// <summary>
    /// 사용자로 페이지 모델을 만듭니다 (활성 사용자만, 없으면 null). 내 페이지면 ID·메일·전화·다국어 이름·접속·그룹·역할·안 읽은 메시지·환경설정까지 채웁니다.
    /// </summary>
    private async Task<UserPageModel?> ModelOfAsync(Guid userId, string tab, bool isMe)
    {
        var db = (DbContext)this.DbContext!;
        var query = db.Set<AppUser>().AsNoTracking().AsSplitQuery().Include(p => p.DpNames).Include(p => p.Properties).Where(p => p.Id == userId && p.Enabler);
        if (isMe) query = query.Include(p => p.UserGroups).ThenInclude(p => p.Group!.DpNames).Include(p => p.UserRoles).ThenInclude(p => p.Role!.DpNames);
        var user = await query.FirstOrDefaultAsync();
        if (user is null) return null;

        var model = new UserPageModel
        {
            Tab = tab,
            IsMe = isMe,
            UserId = user.Id,
            Name = this.Request.DisplayName(user.DpNames, user.UserName),
            Photo = user.Properties.FirstOrDefault(p => p.Key == UserApi.PhotoKey)?.Value ?? string.Empty,
            Joined = user.CDate
        };
        if (!isMe) return model;

        model.UserName = user.UserName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;
        model.EmailConfirmed = user.EmailConfirmed;
        model.Phone = user.PhoneNumber ?? string.Empty;
        model.DpNames = user.DpNames.ToDictionary(p => p.Culture, p => p.Name);
        model.LastAccess = user.LastAccess;
        model.Groups = [.. user.UserGroups.Select(p => (this.Request.DisplayName(p.Group?.DpNames, p.Group?.Name), p.Active, p.Default))];
        model.Roles = [.. user.UserRoles.Select(p => (this.Request.DisplayName(p.Role?.DpNames, p.Role?.Name), p.Active, p.Default))];
        model.Unread = await db.Set<AppUserMessage>().CountAsync(p => p.ReceiverId == userId && p.RDate == null && !p.ReceiverDeleted);
        model.UnreadNotice = await db.Set<AppUserNotice>().CountAsync(p => p.ReceiverId == userId && p.RDate == null);
        model.ReceiveMessage = user.Properties.FirstOrDefault(p => p.Key == UserApi.MessageOffKey)?.Value != "true";
        return model;
    }
}
