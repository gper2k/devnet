using BitPlus.Web.Security.Policy;
using BitPlus.Web.Site.OAuth;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

public partial class MembershipController
{
    /// <summary>등록할 수 있는 외부 로그인 서비스 수</summary>
    private const int MaxOAuthProviders = 20;

    /// <summary>일반 OIDC discovery 문서 조회용 공용 클라이언트</summary>
    private static readonly HttpClient DiscoveryClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    #region OAuth

    /// <summary>
    /// 외부 로그인 서비스 목록을 조회합니다 (설정 "OAuthProviders"). Client Secret 은 내려주지 않고 저장 여부(hasSecret)만, 서비스 콘솔에 등록할 콜백 주소(callbackUrl)를 함께 알려줍니다.
    /// </summary>
    /// <returns>서비스 목록 (순서대로)</returns>
    [HttpGet("oauth")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetOAuthProviders() => this.Complete(OAuthProvider.ListOf(this.Config).OrderBy(p => p.Sort).ThenBy(p => p.Name).Select(p => new
    {
        p.Id, kind = p.Kind.ToString(), p.Name, p.ClientId, hasSecret = !string.IsNullOrEmpty(p.ClientSecret), p.Authority, p.Sort, p.Enabler,
        callbackUrl = $"{this.Request.Scheme}://{this.Request.Host}{p.CallbackPath()}"
    }));

    /// <summary>
    /// 외부 로그인 서비스 하나를 등록하거나 수정합니다 (설정 "OAuthProviders" 목록 안에서 식별키로 교체, 없으면 추가). 저장 즉시 로그인 화면과 콜백에 반영됩니다.
    /// Client Secret 은 비워 두면 기존 값을 유지하고, 입력하면 마스터 키의 용도 키로 암호화합니다. 일반 OIDC 는 발급자 주소의 discovery 문서에서 인증·토큰·사용자 정보 주소를 받아 저장합니다.
    /// </summary>
    /// <param name="id">식별키 (비우면 새 서비스)</param>
    /// <param name="kind">서비스 종류 (Google, Microsoft, Kakao, Naver, Oidc)</param>
    /// <param name="name">로그인 버튼 이름 (필수)</param>
    /// <param name="clientId">Client ID (필수)</param>
    /// <param name="clientSecret">Client Secret (새 서비스는 필수, 비우면 기존 값 유지)</param>
    /// <param name="authority">일반 OIDC 발급자 주소 (https, 일반 OIDC 만 필수)</param>
    /// <param name="sort">로그인 화면 순서</param>
    /// <param name="enabler">사용 여부</param>
    /// <returns>저장 결과 (data = 식별키)</returns>
    [HttpPut("oauth")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveOAuthProvider([FromForm] string? id, [FromForm] OAuthKind kind, [FromForm] string? name, [FromForm] string? clientId,
        [FromForm] string? clientSecret, [FromForm] string? authority, [FromForm] int sort, [FromForm] bool enabler)
    {
        var list = OAuthProvider.ListOf(this.Config);
        var saved = list.FirstOrDefault(p => p.Id == id);
        if (saved is null && list.Count >= MaxOAuthProviders) return this.Error(this.Lang("MaxCountOf").Formats(this.Lang("OAuthProvider"), MaxOAuthProviders.ToString())!);
        var item = new OAuthProvider
        {
            Id = saved?.Id ?? Guid.NewGuid().ToString("N"),
            Kind = kind,
            Name = name.Clip(64),
            ClientId = clientId.Clip(256),
            ClientSecret = string.IsNullOrWhiteSpace(clientSecret) ? saved?.ClientSecret : this.Config!.ProtectSecret(clientSecret.Trim(), OAuthProvider.ProtectPurpose),
            Authority = kind == OAuthKind.Oidc ? authority.Clip(512).TrimEnd('/') : string.Empty,
            Sort = sort,
            Enabler = enabler,
            UDate = DateTime.UtcNow
        };
        if (!Enum.IsDefined(item.Kind)) return this.Error(WebResources.ValidRegex.Formats(this.Lang("OAuthKind"))!);
        if (item.Name.Length == 0) return this.Error(WebResources.ValidRequired.Formats(this.Lang("Name"))!);
        if (item.ClientId.Length == 0) return this.Error(WebResources.ValidRequired.Formats("Client ID")!);
        if (item.Kind != OAuthKind.Kakao && string.IsNullOrEmpty(item.ClientSecret)) return this.Error(WebResources.ValidRequired.Formats("Client Secret")!);
        if (item.Kind == OAuthKind.Oidc && await this.DiscoverOidcAsync(item) is { } discoverError) return this.Error(discoverError);

        var index = saved is null ? -1 : list.IndexOf(saved);
        if (index < 0) list.Add(item);
        else list[index] = item;
        await this.Config!.SetModelAsync(this.DbContext!, OAuthProvider.Key, list);
        return this.Complete(item.Id, WebResources.SavedComplate);
    }

    /// <summary>
    /// 외부 로그인 서비스 하나를 삭제합니다 (설정 "OAuthProviders" 목록에서 제거). 이 서비스로 연결한 회원 계정의 연결 기록은 남지만 그 서비스로는 더 로그인할 수 없습니다.
    /// </summary>
    /// <param name="id">식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("oauth/{id}")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> DeleteOAuthProvider(string id)
    {
        var list = OAuthProvider.ListOf(this.Config);
        if (list.RemoveAll(p => p.Id == id) == 0) return this.NotFoundResult(WebResources.NotFoundData);
        await this.Config!.SetModelAsync(this.DbContext!, OAuthProvider.Key, list);
        return this.Complete(null, WebResources.DeletedComplete);
    }

    /// <summary>
    /// 일반 OIDC 발급자 주소의 discovery 문서(<c>/.well-known/openid-configuration</c>)에서 인증·토큰·사용자 정보 주소를 받아 서비스에 넣습니다.
    /// </summary>
    /// <param name="item">대상 서비스 (발급자 주소 포함)</param>
    /// <returns>오류 메시지 (성공이면 null)</returns>
    private async Task<string?> DiscoverOidcAsync(OAuthProvider item)
    {
        if (!Uri.TryCreate(item.Authority, UriKind.Absolute, out var uri) || uri.Scheme != "https") return WebResources.ValidRegex.Formats(this.Lang("OAuthAuthority"));
        try
        {
            using var json = JsonDocument.Parse(await DiscoveryClient.GetStringAsync($"{item.Authority}/.well-known/openid-configuration", this.HttpContext.RequestAborted));
            string Url(string name) => json.RootElement.TryGetProperty(name, out var value) && value.GetString() is { } url && url.StartsWith("https://", StringComparison.Ordinal) ? url : string.Empty;
            item.AuthorizationEndpoint = Url("authorization_endpoint");
            item.TokenEndpoint = Url("token_endpoint");
            item.UserInfoEndpoint = Url("userinfo_endpoint");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return this.Lang("SetupTestFailed").Formats(ex.Message);
        }
        return item.AuthorizationEndpoint.Length > 0 && item.TokenEndpoint.Length > 0 && item.UserInfoEndpoint.Length > 0
            ? null : this.Lang("SetupTestFailed").Formats("openid-configuration");
    }

    #endregion
}
