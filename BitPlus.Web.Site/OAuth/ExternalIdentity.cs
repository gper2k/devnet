using System.Security.Cryptography;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인 서비스가 확인해 준 사용자 정보. 콜백에서 캐시(10분, 여러 서버 공용)에 두고 HttpOnly 쿠키에 그 키만 담아,
/// 외부 로그인 화면(<c>/Account/External</c>)이 로그인·계정 연결·가입을 마칠 때까지 씁니다.
/// </summary>
public class ExternalIdentity
{
    /// <summary>캐시 키를 담는 쿠키 이름</summary>
    public const string CookieName = "BitPlus.External";

    /// <summary>보관 시간</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// <see cref="ExternalIdentity"/> 기본 생성자.
    /// </summary>
    public ExternalIdentity()
    {
        this.ProviderId = string.Empty;
        this.ProviderName = string.Empty;
        this.Key = string.Empty;
        this.Email = string.Empty;
        this.EmailVerified = false;
        this.Name = string.Empty;
        this.ReturnUrl = null;
    }

    /// <summary>서비스 식별키 (<see cref="OAuthProvider.Id"/>, 로그인 연결의 LoginProvider)</summary>
    public string ProviderId { get; set; }

    /// <summary>서비스 표시 이름</summary>
    public string ProviderName { get; set; }

    /// <summary>서비스 안 사용자 고유 식별자 (로그인 연결의 ProviderKey)</summary>
    public string Key { get; set; }

    /// <summary>메일 (없으면 빈 값)</summary>
    public string Email { get; set; }

    /// <summary>서비스가 메일을 검증했는지</summary>
    public bool EmailVerified { get; set; }

    /// <summary>이름 (없으면 빈 값)</summary>
    public string Name { get; set; }

    /// <summary>로그인 후 돌아갈 사이트 내부 주소</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// 캐시에 두고 키를 쿠키로 내려줍니다.
    /// </summary>
    /// <param name="http">HTTP 컨텍스트</param>
    public async Task SaveAsync(HttpContext http)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await App.Cache.SetAsync(CacheKey(token), this, Lifetime);
        http.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            Path = "/", HttpOnly = true, Secure = http.Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = Lifetime, IsEssential = true
        });
    }

    /// <summary>
    /// 쿠키의 키로 보관된 정보를 읽습니다.
    /// </summary>
    /// <param name="http">HTTP 컨텍스트</param>
    /// <returns>보관된 정보 (없거나 만료면 null)</returns>
    public static async Task<ExternalIdentity?> LoadAsync(HttpContext http)
        => http.Request.Cookies[CookieName] is { Length: 32 } token ? await App.Cache.GetAsync<ExternalIdentity>(CacheKey(token)) : null;

    /// <summary>
    /// 보관된 정보와 쿠키를 지웁니다 (로그인·연결·가입을 마친 뒤).
    /// </summary>
    /// <param name="http">HTTP 컨텍스트</param>
    public static async Task ClearAsync(HttpContext http)
    {
        if (http.Request.Cookies[CookieName] is { Length: 32 } token) await App.Cache.RemoveAsync(CacheKey(token));
        http.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }

    /// <summary>캐시 키</summary>
    private static string CacheKey(string token) => $"account:external:{token}";
}
