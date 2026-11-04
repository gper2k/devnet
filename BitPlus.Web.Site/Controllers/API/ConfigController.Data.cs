using BitPlus.Data.CommonModel;
using BitPlus.Web.Security.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ConfigController
{
    /// <summary>목록 한 페이지 최대 크기</summary>
    private const int MaxPageSize = 100;

    #region 다국어 데이터 (Languages 테이블)

    /// <summary>
    /// 다국어 데이터를 키 기준(범주 + 키)으로 묶어 조회합니다 (범주·키순). 검색어는 범주·키·값에서 찾습니다. 관리자 정책만.
    /// 화면 문구는 리소스 파일(resx)을 먼저 찾고 없을 때 이 테이블을 쓰므로, 리소스에 있는 키는 여기 값이 보이지 않습니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 (1부터)</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>items([{category, key, values({문화권: 값}), description, uDate}]), total</returns>
    [HttpGet("languages")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> GetLanguages([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var db = this.DbContext!;
        var query = db.Languages.AsNoTracking();
        if (keyword.Clip(100) is { Length: > 0 } q) query = query.Where(p => p.Category!.Contains(q) || p.Key.Contains(q) || p.Value.Contains(q));
        var groups = query.GroupBy(p => new { p.Category, p.Key }).Select(g => new { g.Key.Category, g.Key.Key, UDate = g.Max(x => x.UDate) });
        var total = await groups.CountAsync();
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var keys = await groups.OrderBy(p => p.Category).ThenBy(p => p.Key).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync();
        var names = keys.Select(p => p.Key).Distinct().ToList();
        var rows = await db.Languages.AsNoTracking().Where(p => names.Contains(p.Key)).ToListAsync();
        var items = keys.Select(k =>
        {
            var group = rows.Where(p => p.Category == k.Category && p.Key == k.Key).ToList();
            return new { category = k.Category, key = k.Key, values = group.ToDictionary(p => p.Culture, p => p.Value), description = group.Select(p => p.Description).FirstOrDefault(p => p != null), uDate = k.UDate };
        });
        return this.Complete(new { items, total });
    }

    /// <summary>
    /// 다국어 키 하나(범주 + 키)의 문화권별 값을 통째로 저장합니다 (빈 값은 저장하지 않음 — 모두 비우면 삭제). 새로 등록할 때 같은 범주·키가 있으면 거부합니다.
    /// 저장 후 그 키의 다국어 캐시를 지웁니다. 관리자 정책만.
    /// </summary>
    /// <param name="category">범주 (64자 이내, 비우면 Common)</param>
    /// <param name="key">키 (필수, 128자 이내)</param>
    /// <param name="values">문화권별 값 JSON ({문화권: 값}, 사이트 지원 문화권만)</param>
    /// <param name="description">설명</param>
    /// <param name="isNew">새로 등록인지</param>
    /// <returns>처리 결과</returns>
    [HttpPut("language")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveLanguage([FromForm] string? category, [FromForm] string? key, [FromForm] string? values, [FromForm] string? description, [FromForm] bool isNew)
    {
        var (cat, name) = (category.Clip(64) is { Length: > 0 } c ? c : "Common", key.Clip(128));
        if (name.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Key"))!);
        var cultures = this.Config!.Cultures;
        var map = ParseMap(values).Where(p => cultures.Contains(p.Key) && !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key, p => p.Value!.Trim());
        if (map.Count == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Value"))!);

        var db = this.DbContext!;
        var existing = await db.Languages.Where(p => p.Category == cat && p.Key == name).ToListAsync();
        if (isNew && existing.Count > 0) return this.Error(WebResources.ValidAlreadyItem.Formats(this.Lang("Key"))!);
        var cDate = existing.Count > 0 ? existing.Min(p => p.CDate) : DateTime.UtcNow; // 최초 등록 시각은 유지
        db.Languages.RemoveRange(existing);
        foreach (var (culture, text) in map) db.Languages.Add(new LanguageData(name, culture, text, cat, description.Clip(256) is { Length: > 0 } d ? d : null) { CDate = cDate, UDate = DateTime.UtcNow });
        await ((DbContext)db).SaveChangesAsync();
        await this.InvalidateLanguageCacheAsync(name);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 다국어 키 하나(범주 + 키)의 모든 문화권 값을 지우고 캐시를 지웁니다. 관리자 정책만.
    /// </summary>
    /// <param name="category">범주</param>
    /// <param name="key">키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("language")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> DeleteLanguage([FromQuery] string? category, [FromQuery] string? key)
    {
        var db = this.DbContext!;
        var rows = await db.Languages.Where(p => p.Category == (category ?? string.Empty) && p.Key == (key ?? string.Empty)).ToListAsync();
        if (rows.Count == 0) return this.NotFoundResult(WebResources.NotFoundData);
        db.Languages.RemoveRange(rows);
        await ((DbContext)db).SaveChangesAsync();
        await this.InvalidateLanguageCacheAsync(key!);
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>다국어 키의 기본 캐시와 약관·메일 템플릿 전용 캐시를 함께 지웁니다.</summary>
    private async Task InvalidateLanguageCacheAsync(string key)
    {
        await this.Request.RemoveLanguageCacheAsync(key);
        if (key.StartsWith(WebConfig.JoinTermsKey + "@", StringComparison.OrdinalIgnoreCase))
            await App.Cache.RemoveAsync("account:terms:languages");
        else if (key.IndexOf('@') is > 0 and var idx)
            await App.Cache.RemoveAsync($"template:mail:{key[..idx]}");
    }

    #endregion

    #region 설정 데이터 (Configs 테이블)

    /// <summary>
    /// 설정(키-값) 목록을 범주·키순으로 조회합니다. 검색어는 범주·키·설명에서 찾고, 범주를 주면 그 범주만. 마스터 키(<see cref="WebConfig.SecretKeyName"/>)는 목록에 넣지 않고, 비밀 값(비밀번호·비밀 키·API 키)은 <see cref="SecretMask"/> 로 가립니다. 관리자 정책만.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 (1부터)</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>items([{key, category, value, description, enabled, json(값이 JSON 객체·배열인지), uDate}]), total</returns>
    [HttpGet("settings")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> GetSettings([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = this.DbContext!.Settings.AsNoTracking().Where(p => p.Key != WebConfig.SecretKeyName);
        if (keyword.Clip(100) is { Length: > 0 } q) query = query.Where(p => p.Key.Contains(q) || p.Category!.Contains(q) || p.Description!.Contains(q));
        var total = await query.CountAsync();
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows = await query.OrderBy(p => p.Category).ThenBy(p => p.Key).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = rows.Select(p => new { key = p.Key, category = p.Category, value = MaskSecrets(p.Key, p.Value), description = p.Description, enabled = p.Enabled, json = IsJson(p.Value), uDate = p.UDate });
        return this.Complete(new { items, total });
    }

    /// <summary>
    /// 설정 하나를 등록하거나 고칩니다 (키는 고칠 수 없음). 원래 값이 JSON 객체·배열이면 새 값도 올바른 JSON 이어야 합니다(화면 기능이 읽는 모델이 깨지지 않게). 가림 표시(<see cref="SecretMask"/>)가 남은 비밀 값은 원래 값을 유지합니다.
    /// 저장 후 그 범주·키의 설정 캐시(값·모델)를 지웁니다. 마스터 키(<see cref="WebConfig.SecretKeyName"/>)는 다룰 수 없습니다. 관리자 정책만.
    /// </summary>
    /// <param name="key">키 (필수, 64자 이내, 모든 범주에서 유일)</param>
    /// <param name="category">범주 (64자 이내, 비우면 AppConfig)</param>
    /// <param name="value">값</param>
    /// <param name="description">설명</param>
    /// <param name="enabled">사용 여부</param>
    /// <param name="isNew">새로 등록인지</param>
    /// <returns>처리 결과</returns>
    [HttpPut("setting")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveSetting([FromForm] string? key, [FromForm] string? category, [FromForm] string? value, [FromForm] string? description, [FromForm] bool enabled, [FromForm] bool isNew)
    {
        var name = key.Clip(64);
        if (name.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Key"))!);
        if (name == WebConfig.SecretKeyName) return this.ForbiddenResult();
        var db = this.DbContext!;
        var item = await db.Settings.FirstOrDefaultAsync(p => p.Key == name);
        if (isNew && item is not null) return this.Error(WebResources.ValidAlreadyItem.Formats(this.Lang("Key"))!);
        if (!isNew && item is null) return this.NotFoundResult(WebResources.NotFoundData);
        var text = RestoreSecrets(name, value ?? string.Empty, item?.Value);
        if (item is not null && IsJson(item.Value) && !IsJson(text)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("Value"))!);

        var oldCategory = item?.Category;
        var cat = category.Clip(64) is { Length: > 0 } c ? c : "AppConfig";
        if (item is null) db.Settings.Add(new KeyValueData(name, text, cat, description.Clip(256) is { Length: > 0 } d ? d : null, enabled) { CDate = DateTime.UtcNow, UDate = DateTime.UtcNow });
        else (item.Value, item.Category, item.Description, item.Enabled, item.UDate) = (text, cat, description.Clip(256) is { Length: > 0 } d ? d : null, enabled, DateTime.UtcNow);
        await ((DbContext)db).SaveChangesAsync();
        await this.InvalidateSettingAsync(name, cat, oldCategory);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 설정 하나를 지우고 캐시를 지웁니다 (지우면 appsettings 값이 있으면 그 값으로 돌아감). 마스터 키는 지울 수 없습니다. 관리자 정책만.
    /// </summary>
    /// <param name="key">키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("setting/{key}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        if (key == WebConfig.SecretKeyName) return this.ForbiddenResult();
        var db = this.DbContext!;
        var item = await db.Settings.FirstOrDefaultAsync(p => p.Key == key);
        if (item is null) return this.NotFoundResult(WebResources.NotFoundData);
        db.Settings.Remove(item);
        await ((DbContext)db).SaveChangesAsync();
        await this.InvalidateSettingAsync(key, item.Category, null);
        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>설정 캐시(값·모델)를 지금 범주와 (바뀌었으면) 이전 범주에서 지웁니다.</summary>
    private async Task InvalidateSettingAsync(string key, string? category, string? oldCategory)
    {
        foreach (var cat in new[] { category, oldCategory }.OfType<string>().Distinct())
            await this.Config!.ForCategory(cat).InvalidateAsync(key);
    }

    /// <summary>값이 JSON 객체·배열인지</summary>
    private static bool IsJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.TrimStart()[0] is not ('{' or '[')) return false;
        try { return JsonNode.Parse(value) is JsonObject or JsonArray; }
        catch (JsonException) { return false; }
    }

    /// <summary>목록에서 비밀 값 자리에 보여 주는 가림 표시 (저장할 때 이 값이 남아 있으면 원래 값을 유지)</summary>
    private const string SecretMask = "******";

    /// <summary>이름이 비밀 값(비밀번호·비밀 키·API 키)을 뜻하는지</summary>
    private static bool IsSecretName(string name)
        => name.Contains("password", StringComparison.OrdinalIgnoreCase) || name.Contains("secret", StringComparison.OrdinalIgnoreCase) || name.Equals("apiKey", StringComparison.OrdinalIgnoreCase);

    /// <summary>설정 값의 비밀 값을 가립니다: 키 이름이 비밀이면 값 전체, JSON 이면 비밀 이름 속성의 (비어 있지 않은) 문자열 값.</summary>
    private static string? MaskSecrets(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (IsSecretName(key)) return SecretMask;
        if (!IsJson(value)) return value;
        var node = JsonNode.Parse(value);
        MaskNode(node);
        return node!.ToJsonString();
    }

    /// <summary>JSON 노드 안의 비밀 이름 속성 문자열 값을 가림 표시로 바꿉니다 (하위 객체·배열까지).</summary>
    private static void MaskNode(JsonNode? node)
    {
        if (node is JsonArray array) foreach (var child in array) MaskNode(child);
        if (node is not JsonObject obj) return;
        foreach (var (name, child) in obj.ToList())
            if (IsSecretName(name) && child is JsonValue v && v.TryGetValue<string>(out var text) && text.Length > 0) obj[name] = SecretMask;
            else MaskNode(child);
    }

    /// <summary>저장할 값에 남은 가림 표시를 원래 값으로 되돌립니다 (가린 채로 저장해도 비밀 값이 지워지지 않게).</summary>
    private static string RestoreSecrets(string key, string text, string? original)
    {
        if (original is null || !text.Contains(SecretMask)) return text;
        if (IsSecretName(key)) return text == SecretMask ? original : text;
        if (!IsJson(text) || !IsJson(original)) return text;
        var node = JsonNode.Parse(text);
        RestoreNode(node, JsonNode.Parse(original));
        return node!.ToJsonString();
    }

    /// <summary>가림 표시인 문자열 값을 원래 JSON 의 같은 자리 값으로 바꿉니다. ponytail: 배열은 순서(인덱스)로 맞춤, 항목을 옮기며 고치면 다른 항목 값이 들어갈 수 있음 — 필요하면 id 로 맞추기.</summary>
    private static void RestoreNode(JsonNode? node, JsonNode? source)
    {
        if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++) RestoreNode(array[i], source is JsonArray from && i < from.Count ? from[i] : null);
        if (node is not JsonObject obj) return;
        foreach (var (name, child) in obj.ToList())
        {
            var from = (source as JsonObject)?[name];
            if (child is JsonValue v && v.TryGetValue<string>(out var text) && text == SecretMask) obj[name] = from?.DeepClone();
            else RestoreNode(child, from);
        }
    }

    #endregion
}
