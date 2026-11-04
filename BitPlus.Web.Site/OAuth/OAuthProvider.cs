namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인(OAuth) 서비스 하나 (설정 "OAuthProviders" = 이 모델의 목록, AppConfig 범주 — 캐시 → DB → appsettings). 관리자 "계정 관리 > OAuth 구성" 에서 저장합니다.
/// Client Secret 은 마스터 키의 용도 키(<see cref="ProtectPurpose"/>)로 암호화해 두고 화면에는 내려주지 않습니다.
/// 인증 스킴 이름(<see cref="SchemeOf"/>)에 저장 시각이 들어가므로 저장하면 모든 서버가 다음 요청부터 새 값으로 처리합니다(재시작 불필요).
/// </summary>
public class OAuthProvider
{
    /// <summary>설정 키 (서비스 목록)</summary>
    public const string Key = "OAuthProviders";

    /// <summary>Client Secret 암호화 용도 문자열 (<see cref="WebConfig.ProtectSecret"/>)</summary>
    public const string ProtectPurpose = "BitPlus.OAuth.ClientSecret";

    /// <summary>인증 스킴 이름 머리 (<c>oauth.{식별키}.{저장 시각}</c>)</summary>
    public const string SchemePrefix = "oauth.";

    /// <summary>콜백 주소 머리 (<c>/signin-oauth/{식별키}</c>)</summary>
    public const string CallbackPrefix = "/signin-oauth/";

    /// <summary>
    /// <see cref="OAuthProvider"/> 기본 생성자.
    /// </summary>
    public OAuthProvider()
    {
        this.Id = string.Empty;
        this.Kind = OAuthKind.Google;
        this.Name = string.Empty;
        this.ClientId = string.Empty;
        this.ClientSecret = null;
        this.Authority = string.Empty;
        this.AuthorizationEndpoint = string.Empty;
        this.TokenEndpoint = string.Empty;
        this.UserInfoEndpoint = string.Empty;
        this.Sort = 0;
        this.Enabler = true;
        this.UDate = DateTime.MinValue;
    }

    /// <summary>식별키 (목록 안에서 유일, 콜백 주소에 쓰임 — 바뀌지 않음)</summary>
    public string Id { get; set; }

    /// <summary>서비스 종류</summary>
    public OAuthKind Kind { get; set; }

    /// <summary>로그인 버튼에 보일 이름</summary>
    public string Name { get; set; }

    /// <summary>Client ID</summary>
    public string ClientId { get; set; }

    /// <summary>암호화된 Client Secret</summary>
    public string? ClientSecret { get; set; }

    /// <summary>일반 OIDC 발급자 주소 (discovery 문서 = <c>{주소}/.well-known/openid-configuration</c>)</summary>
    public string Authority { get; set; }

    /// <summary>일반 OIDC 인증 주소 (저장할 때 discovery 문서에서 받음)</summary>
    public string AuthorizationEndpoint { get; set; }

    /// <summary>일반 OIDC 토큰 주소 (저장할 때 discovery 문서에서 받음)</summary>
    public string TokenEndpoint { get; set; }

    /// <summary>일반 OIDC 사용자 정보 주소 (저장할 때 discovery 문서에서 받음)</summary>
    public string UserInfoEndpoint { get; set; }

    /// <summary>로그인 화면 버튼 순서 (작은 값 먼저)</summary>
    public int Sort { get; set; }

    /// <summary>사용 여부</summary>
    public bool Enabler { get; set; }

    /// <summary>마지막 저장 시각 (UTC, 스킴 이름에 쓰여 저장하면 옵션을 새로 만듦. appsettings 로 둔 서비스는 비어 있음)</summary>
    public DateTime UDate { get; set; }

    /// <summary>
    /// 종류의 고정 값에 저장된 주소(일반 OIDC)를 반영한 값.
    /// </summary>
    public OAuthPreset Preset()
    {
        var preset = OAuthPreset.Of(this.Kind);
        if (this.Kind != OAuthKind.Oidc) return preset;

        string? brandIcon = null;
        var nameLower = (this.Name + " " + this.Id).ToLowerInvariant();
        if (nameLower.Contains("apple")) brandIcon = "/img/brands/apple.svg";
        else if (nameLower.Contains("github")) brandIcon = "/img/brands/github.svg";
        else if (nameLower.Contains("google")) brandIcon = "/img/brands/google.svg";
        else if (nameLower.Contains("microsoft")) brandIcon = "/img/brands/microsoft.svg";
        else if (nameLower.Contains("kakao")) brandIcon = "/img/brands/kakao.svg";
        else if (nameLower.Contains("naver")) brandIcon = "/img/brands/naver.svg";

        return new OAuthPreset
        {
            AuthorizationEndpoint = this.AuthorizationEndpoint,
            TokenEndpoint = this.TokenEndpoint,
            UserInfoEndpoint = this.UserInfoEndpoint,
            BrandIcon = brandIcon
        };
    }

    /// <summary>인증 스킴 이름 (<c>oauth.{식별키}.{저장 시각}</c>)</summary>
    public string SchemeName() => $"{SchemePrefix}{this.Id}.{this.UDate.Ticks:x}";

    /// <summary>콜백 경로 (<c>/signin-oauth/{식별키}</c>)</summary>
    public string CallbackPath() => CallbackPrefix + this.Id;

    /// <summary>
    /// 저장된 서비스 목록 (없거나 읽기에 실패하면 빈 목록).
    /// </summary>
    /// <param name="config">웹 설정</param>
    public static List<OAuthProvider> ListOf(WebConfig? config)
    {
        try { return config?.GetModel<List<OAuthProvider>>(Key) ?? []; }
        catch (Exception) { return []; }
    }

    /// <summary>
    /// 로그인에 쓸 수 있는 서비스 (사용 중, 순서대로).
    /// </summary>
    /// <param name="config">웹 설정</param>
    public static List<OAuthProvider> EnabledOf(WebConfig? config)
        => [.. ListOf(config).Where(p => p.Enabler).OrderBy(p => p.Sort).ThenBy(p => p.Name)];

    /// <summary>
    /// 인증 스킴 이름으로 사용 중인 서비스를 찾습니다 (저장 시각까지 같아야 함).
    /// </summary>
    /// <param name="config">웹 설정</param>
    /// <param name="scheme">인증 스킴 이름</param>
    public static OAuthProvider? SchemeOf(WebConfig? config, string? scheme)
        => scheme?.StartsWith(SchemePrefix, StringComparison.Ordinal) != true ? null : EnabledOf(config).FirstOrDefault(p => p.SchemeName() == scheme);
}
