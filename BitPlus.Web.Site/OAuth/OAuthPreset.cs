namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인 서비스 종류별 고정 값: 인증·토큰·사용자 정보 주소, 요청 범위, 사용자 정보 응답에서 읽을 항목 경로(점 표기), PKCE 사용 여부.
/// 일반 OIDC 는 주소를 discovery 문서에서 받아 <see cref="OAuthProvider"/> 에 저장하므로 여기 주소는 비어 있습니다.
/// </summary>
public class OAuthPreset
{
    /// <summary>
    /// <see cref="OAuthPreset"/> 기본 생성자 (표준 OIDC 사용자 정보 항목).
    /// </summary>
    public OAuthPreset()
    {
        this.AuthorizationEndpoint = string.Empty;
        this.TokenEndpoint = string.Empty;
        this.UserInfoEndpoint = string.Empty;
        this.Scopes = ["openid", "email", "profile"];
        this.IdPath = "sub";
        this.EmailPath = "email";
        this.VerifiedPath = "email_verified";
        this.NamePath = "name";
        this.UsePkce = true;
        this.Icon = "fiGlobe";
        this.BrandIcon = null;
    }

    /// <summary>브랜드 전용 SVG 아이콘 경로 (예: /img/brands/google.svg)</summary>
    public string? BrandIcon { get; init; }

    /// <summary>인증(동의) 화면 주소</summary>
    public string AuthorizationEndpoint { get; init; }

    /// <summary>토큰 발급 주소</summary>
    public string TokenEndpoint { get; init; }

    /// <summary>사용자 정보 주소 (접근 토큰 Bearer GET)</summary>
    public string UserInfoEndpoint { get; init; }

    /// <summary>요청 범위</summary>
    public string[] Scopes { get; init; }

    /// <summary>사용자 고유 식별자 경로</summary>
    public string IdPath { get; init; }

    /// <summary>메일 경로</summary>
    public string EmailPath { get; init; }

    /// <summary>메일 검증 여부 경로 (빈 값 = 서비스가 알려 주지 않음 → 미검증으로 취급)</summary>
    public string VerifiedPath { get; init; }

    /// <summary>이름 경로 (쉼표로 여러 개 — 앞에서부터 값이 있는 것)</summary>
    public string NamePath { get; init; }

    /// <summary>PKCE 사용 여부</summary>
    public bool UsePkce { get; init; }

    /// <summary>로그인 버튼 아이콘 (Fluent Icons 클래스)</summary>
    public string Icon { get; init; }

    /// <summary>
    /// 종류별 고정 값을 돌려줍니다.
    /// </summary>
    /// <param name="kind">서비스 종류</param>
    public static OAuthPreset Of(OAuthKind kind) => kind switch
    {
        OAuthKind.Google => new()
        {
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo",
            BrandIcon = "/img/brands/google.svg"
        },
        OAuthKind.Microsoft => new()
        {
            AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            UserInfoEndpoint = "https://graph.microsoft.com/oidc/userinfo",
            VerifiedPath = string.Empty,
            BrandIcon = "/img/brands/microsoft.svg"
        },
        OAuthKind.Kakao => new()
        {
            AuthorizationEndpoint = "https://kauth.kakao.com/oauth/authorize",
            TokenEndpoint = "https://kauth.kakao.com/oauth/token",
            UserInfoEndpoint = "https://kapi.kakao.com/v2/user/me",
            Scopes = ["profile_nickname"],
            IdPath = "id",
            EmailPath = "kakao_account.email",
            VerifiedPath = "kakao_account.is_email_verified",
            NamePath = "kakao_account.profile.nickname",
            Icon = "fiChat",
            BrandIcon = "/img/brands/kakao.svg"
        },
        OAuthKind.Naver => new()
        {
            AuthorizationEndpoint = "https://nid.naver.com/oauth2.0/authorize",
            TokenEndpoint = "https://nid.naver.com/oauth2.0/token",
            UserInfoEndpoint = "https://openapi.naver.com/v1/nid/me",
            Scopes = [],
            IdPath = "response.id",
            EmailPath = "response.email",
            VerifiedPath = string.Empty,
            NamePath = "response.name,response.nickname",
            UsePkce = false,
            BrandIcon = "/img/brands/naver.svg"
        },
        _ => new()
    };
}
