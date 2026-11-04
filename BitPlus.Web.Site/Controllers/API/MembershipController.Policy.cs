using System.Text.RegularExpressions;
using BitPlus.Data.Account;
using BitPlus.Data.Account.Model;
using BitPlus.Data.Account.Model.Cache;
using BitPlus.Web.Data.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class MembershipController
{
    #region Policy

    /// <summary>
    /// 권한 정책 목록을 구성 파일(appsettings.json "Policies") 정책과 DB 정책(비활성 포함)을 이름별로 병합해 이름순으로 조회합니다.
    /// source: Config(구성 파일 전용) / Db(DB 전용) / Override(같은 이름의 DB 정책이 구성 파일 정책을 덮어씀), applied: 실제 적용 중인 쪽(Db/Config, 없으면 null = 거부).
    /// </summary>
    /// <returns>정책 목록 [{name, source, applied, id, enabler, description, validFrom, validTo, targets}]</returns>
    [HttpGet("policy/list")]
    public async Task<IActionResult> GetPolicyList()
    {
        var entries = await this.PolicyManager!.GetPolicyEntriesAsync(this.HttpContext.RequestAborted);
        var names = await this.TargetNamesAsync(entries.Where(p => p.Db is not null).SelectMany(p => p.Db!.Targets)
            .Where(p => p.TargetId.HasValue).Select(p => (p.TargetType, p.TargetId!.Value)));
        return this.Complete(entries.Select(p =>
        {
            var shown = p.Effective ?? p.Db!; // 적용 정책이 없으면(비활성 DB 전용) DB 값을 보여 준다
            return new
            {
                name = p.Name,
                source = p.Config is null ? "Db" : p.Db is null ? "Config" : "Override",
                applied = p.Effective is null ? null : p.Effective == p.Db ? "Db" : "Config",
                id = p.Db?.Id,
                enabler = p.Db?.Enabler,
                description = shown.Description,
                validFrom = shown.ValidFrom,
                validTo = shown.ValidTo,
                targets = shown.Targets.Select(t => TargetText(t, names)).ToList()
            };
        }));
    }

    /// <summary>
    /// 권한 정책 하나의 구성 파일 정책(읽기 전용)과 DB 정책을 함께 조회합니다.
    /// 구성 파일 대상은 이름으로 정의되므로 역할/그룹/사용자 식별키를 찾아 key 로 넣습니다 (찾지 못하거나 와일드카드면 key = null) — DB 덮어쓰기 입력의 초기값용.
    /// </summary>
    /// <param name="name">정책명</param>
    /// <returns>name, config({description, enabler, validFrom, validTo, targets}) | null, db({id, description, enabler, isSystem, validFrom, validTo, targets}) | null</returns>
    [HttpGet("policy/item")]
    public async Task<IActionResult> GetPolicy([FromQuery] string? name)
    {
        name = name?.Trim() ?? string.Empty;
        var config = this.PolicyManager!.GetConfigPolicy(name);
        var db = await this.DbContext!.Policies.AsNoTracking().Include(p => p.Targets).FirstOrDefaultAsync(p => p.Name == name);
        if (config is null && db is null) return this.NotFoundResult(WebResources.NotFoundData);

        var dbTargets = db?.Targets.Select(t => (t.TargetType, t.TargetId)).ToList() ?? [];
        var names = await this.TargetNamesAsync(dbTargets);
        return this.Complete(new
        {
            name = db?.Name ?? config!.Name,
            config = config is null ? null : new
            {
                description = config.Description, enabler = config.Enabler, validFrom = config.ValidFrom, validTo = config.ValidTo,
                targets = await this.ConfigTargetsAsync(config)
            },
            db = db is null ? null : new
            {
                id = db.Id, description = db.Description, enabler = db.Enabler, isSystem = db.IsSystem, validFrom = db.ValidFrom, validTo = db.ValidTo,
                targets = dbTargets.Select(t => new { type = t.TargetType.ToString(), key = (Guid?)t.TargetId, name = names.GetValueOrDefault(t) ?? t.TargetId.ToString() })
            }
        });
    }

    /// <summary>
    /// DB 권한 정책을 등록합니다. 구성 파일에 같은 이름의 정책이 있으면 그 정책을 덮어쓰는 정책이 됩니다 (구성 파일 정책은 읽기 전용).
    /// </summary>
    /// <param name="name">정책명 (영문·숫자·._-, 64자 이내, 생성 후 변경 불가)</param>
    /// <param name="description">설명 (256자 이내)</param>
    /// <param name="start">유효 시작일시 (ISO 8601 UTC, 비우면 즉시)</param>
    /// <param name="end">유효 만료일시 (ISO 8601 UTC, 비우면 영구)</param>
    /// <param name="targets">허용 대상 JSON 배열 [{type, key}] (비우면 모든 사용자 허용)</param>
    /// <param name="enabler">활성 여부 (비활성이면 같은 이름의 구성 파일 정책이 적용)</param>
    /// <returns>저장된 정책 식별키</returns>
    [HttpPost("policy")]
    public async Task<IActionResult> CreatePolicy([FromForm] string? name, [FromForm] string? description, [FromForm] string? start, [FromForm] string? end,
        [FromForm] string? targets, [FromForm] bool enabler)
    {
        name = name?.Trim() ?? string.Empty;
        if (!PolicyNamePattern().IsMatch(name)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("PolicyName"))!);
        var db = this.DbContext!;
        if (await db.Policies.AnyAsync(p => p.Name == name)) return this.Error(WebResources.ValidAlreadyItem.Formats(this.Lang("PolicyName"))!);

        var policy = new AppPolicy(name);
        if (this.ApplyPolicy(policy, description, start, end, targets, enabler) is { } error) return this.Error(error);
        if (!await this.KeepsManagerAccessAsync(name, policy)) return this.Error(this.Lang("PolicyLockOut"));

        db.Policies.Add(policy);
        await ((DbContext)db).SaveChangesAsync();
        await this.PolicyManager!.InvalidatePolicyCacheAsync(name);
        return this.Complete(new { id = policy.Id }, WebResources.AddedComplete);
    }

    /// <summary>
    /// DB 권한 정책을 수정합니다 (정책명은 변경되지 않음).
    /// </summary>
    /// <param name="id">정책 식별키</param>
    /// <param name="description">설명 (256자 이내)</param>
    /// <param name="start">유효 시작일시 (ISO 8601 UTC, 비우면 즉시)</param>
    /// <param name="end">유효 만료일시 (ISO 8601 UTC, 비우면 영구)</param>
    /// <param name="targets">허용 대상 JSON 배열 [{type, key}] (비우면 모든 사용자 허용)</param>
    /// <param name="enabler">활성 여부 (비활성이면 같은 이름의 구성 파일 정책이 적용)</param>
    /// <returns>저장된 정책 식별키</returns>
    [HttpPut("policy/{id:guid}")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromForm] string? description, [FromForm] string? start, [FromForm] string? end,
        [FromForm] string? targets, [FromForm] bool enabler)
    {
        var db = this.DbContext!;
        var policy = await db.Policies.Include(p => p.Targets).FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (this.ApplyPolicy(policy, description, start, end, targets, enabler) is { } error) return this.Error(error);
        if (!await this.KeepsManagerAccessAsync(policy.Name, policy)) return this.Error(this.Lang("PolicyLockOut"));

        policy.UDate = DateTime.UtcNow;
        await ((DbContext)db).SaveChangesAsync();
        await this.PolicyManager!.InvalidatePolicyCacheAsync(policy.Name);
        return this.Complete(new { id = policy.Id }, WebResources.UpdatedComplete);
    }

    /// <summary>
    /// DB 권한 정책의 활성 여부만 바꿉니다 (목록의 활성 토글). 비활성이면 같은 이름의 구성 파일 정책이 적용됩니다.
    /// </summary>
    /// <param name="id">정책 식별키</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPatch("policy/{id:guid}/enabler")]
    public async Task<IActionResult> SetPolicyEnabler(Guid id, [FromForm] bool enabler)
    {
        var db = this.DbContext!;
        var policy = await db.Policies.Include(p => p.Targets).FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null) return this.NotFoundResult(WebResources.NotFoundData);
        policy.Enabler = enabler;
        if (!await this.KeepsManagerAccessAsync(policy.Name, policy)) return this.Error(this.Lang("PolicyLockOut"));

        policy.UDate = DateTime.UtcNow;
        await ((DbContext)db).SaveChangesAsync();
        await this.PolicyManager!.InvalidatePolicyCacheAsync(policy.Name);
        return this.Complete(); // 목록 토글은 조용히 바뀌므로 완료 안내 문구를 보내지 않는다
    }

    /// <summary>
    /// DB 권한 정책을 삭제합니다 (대상 규칙 포함, 시스템 정책은 거부). 같은 이름의 구성 파일 정책이 있으면 그 정책이 다시 적용됩니다.
    /// </summary>
    /// <param name="id">정책 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("policy/{id:guid}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var db = this.DbContext!;
        var policy = await db.Policies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (policy.IsSystem) return this.Error(WebResources.SystemDelWarning);
        if (!await this.KeepsManagerAccessAsync(policy.Name, null)) return this.Error(this.Lang("PolicyLockOut"));

        db.Policies.Remove(policy);
        await ((DbContext)db).SaveChangesAsync();
        await this.PolicyManager!.InvalidatePolicyCacheAsync(policy.Name);
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 입력값을 검증해 DB 정책 엔티티에 반영합니다 (대상은 전달된 목록 기준 병합: 빠진 항목 삭제, 새 항목 추가).
    /// </summary>
    /// <param name="policy">대상 정책 엔티티 (대상 규칙 포함 로드)</param>
    /// <param name="description">설명</param>
    /// <param name="start">유효 시작일시 문자열</param>
    /// <param name="end">유효 만료일시 문자열</param>
    /// <param name="targets">허용 대상 JSON 배열</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>오류 메시지 (성공이면 null)</returns>
    private string? ApplyPolicy(AppPolicy policy, string? description, string? start, string? end, string? targets, bool enabler)
    {
        var (from, to) = (ContentManager.ParseUtc(start), ContentManager.ParseUtc(end));
        if (from.HasValue && to.HasValue && to < from) return this.Lang("PeriodInvalid");

        (policy.Description, policy.ValidFrom, policy.ValidTo, policy.Enabler) = (description.Clip(256), from, to, enabler);
        var picked = ParseTargets(targets);
        policy.Targets.RemoveAll(p => !picked.Contains((p.TargetType, p.TargetId)));
        foreach (var (type, key) in picked)
            if (!policy.Targets.Any(p => p.TargetType == type && p.TargetId == key)) policy.Targets.Add(new AppPolicyTarget(policy.Id, type, key));
        return null;
    }

    /// <summary>
    /// 관리자 정책(<see cref="WebConfig.ManagerPolicyName"/>)을 바꾸는 경우, 바뀐 뒤 적용될 정책으로도 현재 사용자가 관리 권한을 유지하는지 검사합니다 (스스로 잠김 방지).
    /// </summary>
    /// <param name="name">바꾸는 정책명</param>
    /// <param name="dbPolicy">바뀐 뒤의 DB 정책 (삭제면 null)</param>
    /// <returns>관리 권한이 유지되거나 관리자 정책이 아니면 true</returns>
    private async Task<bool> KeepsManagerAccessAsync(string name, AppPolicy? dbPolicy)
    {
        if (!name.NEqual(WebConfig.ManagerPolicyName)) return true;
        var assets = await this.GetCurrentUserAssetsAsync();
        var entry = new PolicyEntry(name, this.PolicyManager!.GetConfigPolicy(name), dbPolicy is null ? null : PolicyCache.FromEntity(dbPolicy));
        return assets is not null && entry.Effective?.IsAllowed(assets) == true;
    }

    /// <summary>
    /// 구성 파일 정책의 대상 목록을 화면용({type, key, name})으로 바꿉니다. 이름으로 정의된 대상은 역할/그룹/사용자 식별키를 찾아 key 에 넣습니다.
    /// </summary>
    /// <param name="config">구성 파일 정책</param>
    /// <returns>대상 목록 (와일드카드나 찾지 못한 대상은 key = null)</returns>
    private async Task<List<object>> ConfigTargetsAsync(PolicyCache config)
    {
        var db = this.IdentityDb;
        var byName = config.Targets.Where(t => !t.TargetId.HasValue && !string.IsNullOrWhiteSpace(t.TargetName)).ToList();
        var roleNames = byName.Where(t => t.TargetType == AccountTypes.Role).Select(t => t.TargetName!).ToList();
        var groupNames = byName.Where(t => t.TargetType is AccountTypes.Group or AccountTypes.Groups).Select(t => t.TargetName!).ToList();
        var userNames = byName.Where(t => t.TargetType == AccountTypes.User).Select(t => t.TargetName!).ToList();
        var roles = roleNames.Count == 0 ? [] : await db.Roles.AsNoTracking().Where(p => roleNames.Contains(p.Name!)).Select(p => new { p.Id, p.Name }).ToListAsync();
        var groups = groupNames.Count == 0 ? [] : await db.Groups.AsNoTracking().Where(p => groupNames.Contains(p.Name!)).Select(p => new { p.Id, p.Name }).ToListAsync();
        var users = userNames.Count == 0 ? [] : await db.Users.AsNoTracking().Where(p => userNames.Contains(p.UserName!)).Select(p => new { p.Id, Name = p.UserName }).ToListAsync();

        Guid? KeyOf(PolicyTargetCache t) => t.TargetId ?? (t.TargetType switch
        {
            AccountTypes.Role => roles.FirstOrDefault(p => p.Name.NEqual(t.TargetName))?.Id,
            AccountTypes.Group or AccountTypes.Groups => groups.FirstOrDefault(p => p.Name.NEqual(t.TargetName))?.Id,
            _ => users.FirstOrDefault(p => p.Name.NEqual(t.TargetName))?.Id
        });

        var keyed = config.Targets.Select(t => (Target: t, Key: KeyOf(t))).ToList();
        var names = await this.TargetNamesAsync(keyed.Where(p => p.Key.HasValue).Select(p => (p.Target.TargetType, p.Key!.Value)));
        return keyed.Select(p => (object)new
        {
            type = p.Target.TargetType.ToString(),
            key = p.Key,
            name = (p.Key.HasValue ? names.GetValueOrDefault((p.Target.TargetType, p.Key.Value)) : null) ?? p.Target.TargetName ?? p.Key?.ToString() ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// 정책 대상 하나의 표시 텍스트 (식별키면 표시명, 이름이면 그 이름).
    /// </summary>
    /// <param name="target">정책 대상</param>
    /// <param name="names">(대상 형식, 대상 키) → 표시명</param>
    /// <returns>표시 텍스트</returns>
    private static string TargetText(PolicyTargetCache target, Dictionary<(AccountTypes, Guid), string> names)
        => (target.TargetId.HasValue ? names.GetValueOrDefault((target.TargetType, target.TargetId.Value)) : null)
            ?? target.TargetName ?? target.TargetId?.ToString() ?? string.Empty;

    /// <summary>정책명 형식: 영문·숫자·마침표·밑줄·하이픈 1~64자</summary>
    [GeneratedRegex(@"^[A-Za-z0-9._\-]{1,64}$")]
    private static partial Regex PolicyNamePattern();

    #endregion
}
