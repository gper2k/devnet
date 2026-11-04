namespace BitPlus.Web.Site.Models;

/// <summary>
/// 외부 서비스 연동 정보 (설정 "SiteIntegration", AppConfig 범주 — 캐시 → DB → appsettings). 관리자 "시스템 설정 > 외부 연동" 에서 저장합니다.
/// 여기 값은 화면(브라우저)에 내려가는 공개용 키만 둡니다 (비밀 키는 두지 않음).
/// </summary>
public class SiteIntegration
{
    /// <summary>설정 키</summary>
    public const string Key = "SiteIntegration";

    /// <summary>
    /// <see cref="SiteIntegration"/> 기본 생성자 (모두 비어 있음 = 연동 안 함)
    /// </summary>
    public SiteIntegration()
    {
        this.KakaoJsKey = string.Empty;
        this.GaMeasurementId = string.Empty;
    }

    /// <summary>카카오 JavaScript 키 (있으면 글 보기 공유 메뉴에 카카오톡 공유)</summary>
    public string KakaoJsKey { get; set; }

    /// <summary>Google Analytics 4 측정 ID (<c>G-XXXXXXXXXX</c>, 있으면 공용 레이아웃이 gtag 스크립트를 넣음 — 관리자 화면 제외)</summary>
    public string GaMeasurementId { get; set; }

    /// <summary>
    /// 저장된 연동 정보 (없거나 읽기에 실패하면 빈 값).
    /// </summary>
    /// <param name="config">웹 설정</param>
    public static SiteIntegration Of(WebConfig? config)
    {
        try { return config?.GetModel<SiteIntegration>(Key) ?? new SiteIntegration(); }
        catch (Exception) { return new SiteIntegration(); }
    }
}
