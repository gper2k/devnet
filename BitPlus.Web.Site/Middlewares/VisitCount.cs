namespace BitPlus.Web.Site.Middlewares;

/// <summary>
/// 방문 집계 값 (페이지 요청 수, 방문자 수) — 일·월·연 카운터 파일 한 개의 내용.
/// </summary>
public class VisitCount
{
    /// <summary>페이지 요청 수</summary>
    public long Pages { get; set; }

    /// <summary>방문자 수 (그날 처음 온 방문자 = 하루 쿠키 발급 수)</summary>
    public long Visitors { get; set; }

    /// <summary>
    /// 빈 집계 값을 만듭니다.
    /// </summary>
    public VisitCount()
    {
        this.Pages = 0;
        this.Visitors = 0;
    }

    /// <summary>
    /// 다른 집계 값을 더합니다.
    /// </summary>
    /// <param name="other">더할 값</param>
    public void Add(VisitCount other)
    {
        this.Pages += other.Pages;
        this.Visitors += other.Visitors;
    }
}
