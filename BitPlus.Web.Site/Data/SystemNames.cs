namespace BitPlus.Web.Site.Data;

/// <summary>
/// 설치 시 등록하는 시스템 역할·그룹의 이름. 설치 seed 와 첫 가입자 처리에서만 쓰입니다.
/// 권한 판단은 역할 이름이 아니라 권한 정책(<see cref="WebConfig.ManagerPolicyName"/>, appsettings.json 의 Policies)이 하므로 프레임워크(WebConfig)에는 두지 않습니다.
/// </summary>
public static class SystemNames
{
    /// <summary>최종 관리자 역할명 (처음 가입한 사용자에게 부여)</summary>
    public const string SystemManagerRole = "SystemManager";

    /// <summary>일반 관리자 역할명 (관리자 그룹에 연결, 관리자 메뉴 접근 대상)</summary>
    public const string ManagerRole = "Manager";

    /// <summary>계정 관리자 역할명 (계정 관리자 그룹에 연결)</summary>
    public const string AccountManagerRole = "AccountManager";

    /// <summary>컨텐츠 관리자 역할명 (컨텐츠 관리자 그룹에 연결, 컨텐츠 관리자 정책 대상)</summary>
    public const string ContentManagerRole = "ContentManager";

    /// <summary>일반 회원 역할명 (가입 기본 역할)</summary>
    public const string MemberRole = "Member";

    /// <summary>관리자 그룹명 (처음 가입한 사용자를 넣음, 일반 관리자 역할이 연결됨)</summary>
    public const string ManagerGroup = "Manager";

    /// <summary>계정 관리자 그룹명 (관리자 그룹 아래, 계정 관리자 역할이 연결됨)</summary>
    public const string AccountManagerGroup = "AccountManager";

    /// <summary>컨텐츠 관리자 그룹명 (관리자 그룹 아래, 컨텐츠 관리자 역할이 연결됨)</summary>
    public const string ContentManagerGroup = "ContentManager";

    /// <summary>일반 회원 그룹명 (가입 기본 그룹)</summary>
    public const string MemberGroup = "Member";

    /// <summary>하단 메뉴(사이트 하단 링크 — 이용약관·개인정보 처리방침 등) 최상위 시스템 메뉴 키</summary>
    public const string FooterMenu = "Footer";
}
