using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 등록된 인증 스킴에 저장된 외부 로그인 서비스(사용 중)를 더해 돌려주는 스킴 공급자.
/// 서비스 목록을 설정(캐시)에서 바로 읽으므로 관리 화면에서 추가·수정·삭제하면 재시작 없이, 여러 서버 모두 다음 요청부터 반영됩니다.
/// 기본 인증 스킴은 없습니다 — 로그인 사용자는 <c>UseAccount</c> 가 자체 토큰으로 넣고, 외부 로그인 처리기는 콜백 주소에서만 동작합니다.
/// </summary>
public class OAuthSchemeProvider : AuthenticationSchemeProvider
{
    private readonly WebConfig config;
    private readonly IHttpContextAccessor accessor;

    /// <summary>
    /// <see cref="OAuthSchemeProvider"/> 생성자.
    /// </summary>
    /// <param name="options">인증 옵션</param>
    /// <param name="config">웹 설정</param>
    /// <param name="accessor">현재 요청 (콜백 주소일 때만 요청 처리 스킴에 외부 로그인을 더함)</param>
    public OAuthSchemeProvider(IOptions<AuthenticationOptions> options, WebConfig config, IHttpContextAccessor accessor) : base(options)
    {
        this.config = config;
        this.accessor = accessor;
    }

    /// <inheritdoc/>
    public override async Task<AuthenticationScheme?> GetSchemeAsync(string name)
        => await base.GetSchemeAsync(name) ?? (OAuthProvider.SchemeOf(this.config, name) is { } provider ? SchemeOf(provider) : null);

    /// <inheritdoc/>
    public override async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
        => [.. await base.GetAllSchemesAsync(), .. this.OAuthSchemes()];

    /// <summary>
    /// 요청마다 <c>UseAuthentication</c> 이 묻는 요청 처리 스킴. 외부 로그인 스킴은 콜백 주소(<c>/signin-oauth/…</c>) 요청일 때만 더해 다른 요청은 설정을 읽지 않습니다.
    /// </summary>
    public override async Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
    {
        var schemes = await base.GetRequestHandlerSchemesAsync();
        return this.accessor.HttpContext?.Request.Path.StartsWithSegments(OAuthProvider.CallbackPrefix.TrimEnd('/')) == true ? [.. schemes, .. this.OAuthSchemes()] : schemes;
    }

    /// <summary>사용 중인 외부 로그인 서비스의 스킴</summary>
    private IEnumerable<AuthenticationScheme> OAuthSchemes() => OAuthProvider.EnabledOf(this.config).Select(SchemeOf);

    /// <summary>서비스 하나의 스킴 (처리기 = 기본 OAuth 처리기)</summary>
    private static AuthenticationScheme SchemeOf(OAuthProvider provider) => new(provider.SchemeName(), provider.Name, typeof(OAuthHandler<OAuthOptions>));
}
