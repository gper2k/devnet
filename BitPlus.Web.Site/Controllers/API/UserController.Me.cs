using BitPlus.Data.Account.Model;
using BitPlus.Data.CommonModel;
using BitPlus.Web.HTTP.Support;
using BitPlus.Web.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class UserController
{
    /// <summary>사용자 이미지 속성 키 (시스템 속성, 값 = 에디터 이미지 경로)</summary>
    public const string PhotoKey = BitPlus.Web.Data.Content.ContentManager.UserPhotoKey;

    /// <summary>메시지 받지 않음 속성 키 (시스템 속성, 값 "true" 면 다른 사용자가 메시지를 보낼 수 없음)</summary>
    public const string MessageOffKey = "MessageOff";

    /// <summary>화면 언어 속성 키 (시스템 속성, 값 = 문화권 코드 — 가입·환경설정 때 저장, 로그인하면 Culture 쿠키로 적용, 서버가 사용자에게 보내는 메일·알림 언어)</summary>
    public const string CultureKey = "Culture";

    /// <summary>테마 속성 키 (시스템 속성, 값 = light / dark — 가입·환경설정 때 저장, 로그인하면 Theme 쿠키로 적용)</summary>
    public const string ThemeKey = "Theme";

    /// <summary>사용자 이미지를 에디터 파일로 올릴 때의 소유자 형식 (소유자 키 = 사용자 식별키)</summary>
    public const string PhotoOwnerType = "user.photo";

    /// <summary>
    /// 내 활성 그룹·역할(지금 활동하는 그룹·역할, Active)을 바꿉니다. 역할을 고르면 그 역할을, 고르지 않으면 그 그룹의 역할 중 기본으로 지정된 역할이 내 역할에도 있을 때 그 역할을 활성으로 바꿉니다.
    /// (화면은 그룹을 고를 때 기본 역할을 미리 골라 주고, 사용자가 다른 역할로 바꿀 수 있습니다.)
    /// </summary>
    /// <param name="groupId">활성으로 바꿀 그룹 식별키 (내 소속 그룹, 없으면 그룹은 그대로)</param>
    /// <param name="roleId">활성으로 바꿀 역할 식별키 (내 역할, 없으면 그룹의 기본 역할)</param>
    /// <returns>처리 결과 (화면이 새로 고침)</returns>
    [HttpPost("group")]
    public async Task<IActionResult> ActivateGroup([FromForm] Guid? groupId, [FromForm] Guid? roleId)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        var (ok, role) = await PickGroupRoleAsync(db, me, groupId, roleId);
        if (!ok) return this.NotFoundResult();

        if (groupId is { } group) await this.AccountManager!.SetActiveGroupAsync(me, group);
        if (role is { } r) await this.AccountManager!.SetActiveRoleAsync(me, r);
        return this.Complete();
    }

    /// <summary>
    /// 내 기본 그룹·역할(주 소속 — 글쓴이 이름 옆 등에 표시, Default)을 바꿉니다. 역할을 고르지 않으면 그 그룹의 역할 중 기본으로 지정된 역할이 내 역할에도 있을 때 그 역할을 기본으로 바꿉니다.
    /// </summary>
    /// <param name="groupId">기본으로 지정할 그룹 식별키 (내 소속 그룹, 없으면 그룹은 그대로)</param>
    /// <param name="roleId">기본으로 지정할 역할 식별키 (내 역할, 없으면 그룹의 기본 역할)</param>
    /// <returns>처리 결과 (화면이 새로 고침)</returns>
    [HttpPost("group/default")]
    public async Task<IActionResult> SetDefaultGroup([FromForm] Guid? groupId, [FromForm] Guid? roleId)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        var db = (DbContext)this.DbContext!;
        var (ok, role) = await PickGroupRoleAsync(db, me, groupId, roleId);
        if (!ok) return this.NotFoundResult();

        var now = DateTime.UtcNow;
        if (groupId is { } group)
            foreach (var link in await db.Set<AppUserGroup>().Where(p => p.UserId == me).ToListAsync())
                (link.Default, link.UDate) = (link.GroupId == group, now);
        if (role is { } r)
            foreach (var link in await db.Set<AppUserRole>().Where(p => p.UserId == me).ToListAsync())
                (link.Default, link.UDate) = (link.RoleId == r, now);
        await db.SaveChangesAsync();
        await this.AccountManager!.InvalidateUserCacheAsync(me);
        return this.Complete();
    }

    /// <summary>
    /// 그룹별 기본 역할(그룹 역할 중 기본으로 지정된 역할)을 모읍니다 — 그룹·역할 지정 창이 그룹을 고를 때 역할을 미리 골라 주는 데 씁니다.
    /// </summary>
    /// <param name="db">DB 컨텍스트</param>
    /// <param name="groupIds">그룹 식별키 목록</param>
    /// <returns>그룹 식별키 → 기본 역할 식별키 (기본 역할이 없는 그룹은 빠짐)</returns>
    public static async Task<Dictionary<Guid, Guid>> DefaultRolesOfAsync(DbContext db, IEnumerable<Guid> groupIds)
    {
        var ids = groupIds.ToList();
        return (await db.Set<AppGroupRole>().AsNoTracking().Where(p => ids.Contains(p.GroupId) && p.Default).Select(p => new { p.GroupId, p.RoleId }).ToListAsync())
            .GroupBy(p => p.GroupId).ToDictionary(g => g.Key, g => g.First().RoleId);
    }

    /// <summary>
    /// 그룹·역할 지정 입력을 확인합니다: 그룹·역할은 내 것이어야 하고(아니면 Ok=false), 역할을 고르지 않았으면 그룹의 기본 역할이 내 역할에 있을 때 그 역할로 정합니다.
    /// </summary>
    /// <returns>Ok: 입력이 올바른지, Role: 적용할 역할 (없으면 null — 역할은 그대로)</returns>
    private static async Task<(bool Ok, Guid? Role)> PickGroupRoleAsync(DbContext db, Guid me, Guid? groupId, Guid? roleId)
    {
        if (groupId is { } group && !await db.Set<AppUserGroup>().AnyAsync(p => p.UserId == me && p.GroupId == group)) return (false, null);
        if (roleId is { } role && !await db.Set<AppUserRole>().AnyAsync(p => p.UserId == me && p.RoleId == role)) return (false, null);
        if (roleId is not null || groupId is not { } g) return (true, roleId);
        var fallback = (await DefaultRolesOfAsync(db, [g])).GetValueOrDefault(g);
        return (true, fallback != Guid.Empty && await db.Set<AppUserRole>().AnyAsync(p => p.UserId == me && p.RoleId == fallback) ? fallback : null);
    }
    /// <summary>사용자 이미지 긴 변의 최대 픽셀 (화면에는 작게만 쓰이므로 큰 이미지는 이 크기로 줄여 WebP 로 저장)</summary>
    private const int PhotoEdge = 256;

    /// <summary>
    /// 내 이미지를 등록/교체합니다. 올린 이미지는 긴 변 <see cref="PhotoEdge"/> 픽셀 이하로 줄여 WebP 로 바꾼 뒤 에디터 파일(소유자 <see cref="PhotoOwnerType"/>, 키 = 내 식별키)로 저장하고,
    /// 쓰이지 않게 된 이전 이미지는 지웁니다. 줄일 수 없는 형식(avif, svg 등)이나 손상된 이미지는 거부합니다.
    /// </summary>
    /// <param name="file">이미지 파일 (폼 필드 file)</param>
    /// <returns>처리 결과</returns>
    [HttpPost("photo")]
    public async Task<IActionResult> SavePhoto(IFormFile? file)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        if (file is null || file.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Image"))!);

        byte[]? small;
        try
        {
            await using var input = file.OpenReadStream();
            small = ImageOptimizer.Fit(input, Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant(), PhotoEdge, 85);
        }
        catch (Exception) { small = null; } // 손상된 이미지
        if (small is null) return this.Error(WebResources.FileTypeNotAllowed.Formats(file.FileName)!);

        var photo = new FormFile(new MemoryStream(small), 0, small.Length, "file", "photo.webp") { Headers = new HeaderDictionary(), ContentType = "image/webp" };
        var (saved, errorFile, sizeError) = await EditorFile.SaveAsync(this.DbContext!, this.Config!.UploadFolder, [photo], PhotoOwnerType, me.ToString(), me, null, this.ClientIP ?? string.Empty);
        if (errorFile is not null || saved.Count == 0) return this.Error((sizeError ? WebResources.FileSizeOver : WebResources.FileTypeNotAllowed).Formats(file.FileName)!);
        return await this.SetPhotoAsync(me, EditorFile.GetUrl(saved[0].Id, saved[0].CDate, saved[0].FileMime));
    }
    /// <summary>
    /// 내 이미지를 지워 기본 아이콘으로 되돌립니다 (등록한 이미지 파일도 지움).
    /// </summary>
    /// <returns>처리 결과</returns>
    [HttpDelete("photo")]
    public async Task<IActionResult> ResetPhoto() => this.CurrentUserId is { } me ? await this.SetPhotoAsync(me, null) : this.UnauthorizedResult();

    /// <summary>
    /// 내 환경설정을 저장합니다 (보낸 항목만): 메시지 받기 여부, 화면 언어(사이트 지원 문화권만), 테마(light / dark).
    /// </summary>
    /// <param name="receiveMessage">다른 사용자의 메시지 받기</param>
    /// <param name="culture">화면 언어 (문화권 코드)</param>
    /// <param name="theme">테마 (light, dark)</param>
    /// <returns>처리 결과</returns>
    [HttpPut("setting")]
    public async Task<IActionResult> SaveSetting([FromForm] bool? receiveMessage, [FromForm] string? culture, [FromForm] string? theme)
    {
        if (this.CurrentUserId is not { } me) return this.UnauthorizedResult();
        if (culture is not null && !(this.Config?.Cultures ?? []).Contains(culture)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("Language"))!);
        if (theme is not null and not ("light" or "dark")) return this.Error(WebResources.ValidRegex.Formats(this.Lang("Theme"))!);
        if (receiveMessage is { } receive) await this.SetPropertyAsync(me, MessageOffKey, receive ? null : "true");
        if (culture is not null) await this.SetPropertyAsync(me, CultureKey, culture);
        if (theme is not null) await this.SetPropertyAsync(me, ThemeKey, theme);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>이미지 속성을 바꾸고(null 이면 삭제) 쓰인 이미지만 에디터 파일로 남깁니다.</summary>
    private async Task<IActionResult> SetPhotoAsync(Guid me, string? photo)
    {
        await this.SetPropertyAsync(me, PhotoKey, photo);
        await EditorFile.SyncAsync(this.DbContext!, this.Config!.UploadFolder, PhotoOwnerType, me.ToString(), photo is null ? string.Empty : $"<img src=\"{photo}\">", false);
        return this.Complete();
    }

    /// <summary>사용자의 시스템 속성을 저장하거나(null 이면 삭제) 사용자 캐시를 지웁니다.</summary>
    private async Task SetPropertyAsync(Guid userId, string key, string? value)
    {
        var db = (DbContext)this.DbContext!;
        var set = db.Set<Property<AppUser, Guid>>();
        var property = await set.FirstOrDefaultAsync(p => p.OwnerID == userId && p.Key == key);
        if (value is null && property is not null) set.Remove(property);
        else if (value is not null && property is null) set.Add(new Property<AppUser, Guid>(userId, key, value, true));
        else if (value is not null && property is not null) (property.Value, property.UDate) = (value, DateTime.UtcNow);
        await db.SaveChangesAsync();
        await this.AccountManager!.InvalidateUserCacheAsync(userId);
    }

}
