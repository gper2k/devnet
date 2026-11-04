namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인 서비스 종류 (<see cref="OAuthProvider.Kind"/>). 종류마다 주소·범위·사용자 정보 항목은 <see cref="OAuthPreset"/> 이 정합니다.
/// </summary>
public enum OAuthKind
{
    /// <summary>Google</summary>
    Google,

    /// <summary>Microsoft (개인·회사·학교 계정)</summary>
    Microsoft,

    /// <summary>Kakao</summary>
    Kakao,

    /// <summary>Naver</summary>
    Naver,

    /// <summary>표준 OpenID Connect (Okta, Keycloak, Auth0 등 — 주소의 discovery 문서로 구성)</summary>
    Oidc
}
