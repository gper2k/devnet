using BitPlus.Web.Services;

namespace BitPlus.Web.Site.Middlewares;

/// <summary>
/// 일간 방문 집계(<see cref="VisitCounter"/>) 등록·미들웨어 확장.
/// </summary>
public static class VisitCounterExtend
{
    /// <summary>하루 방문 쿠키 이름 (값 = 발급한 날짜 yyyyMMdd, 그날 자정까지 유효)</summary>
    public const string CookieName = "Visit";

    /// <summary>
    /// 방문 집계기를 싱글턴으로 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>체이닝용 서비스 컬렉션</returns>
    public static IServiceCollection AddVisitCounter(this IServiceCollection services) => services.AddSingleton<VisitCounter>();

    /// <summary>
    /// 페이지 요청(GET, HTML 을 받는 요청, api·파일 경로 제외)마다 일간 페이지 수를 세고,
    /// 오늘 날짜의 방문 쿠키가 없으면(그날 첫 방문) 쿠키를 발급하며 방문자 수를 셉니다. 정적 파일 뒤에 등록합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    /// <returns>체이닝용 앱</returns>
    public static IApplicationBuilder UseVisitCounter(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (IsPage(context.Request)) Count(context);
        await next(context);
    });

    /// <summary>화면(페이지) 요청인지: GET, Accept 에 text/html, api 경로·확장자 있는 경로가 아님</summary>
    private static bool IsPage(HttpRequest request)
        => HttpMethods.IsGet(request.Method)
            && !request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !Path.HasExtension(request.Path.Value)
            && request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    /// <summary>방문 쿠키의 날짜가 오늘이 아니면 새로 발급(그날 자정 만료)하고, 페이지 요청과 (새 방문이면) 방문자를 셉니다.</summary>
    private static void Count(HttpContext context)
    {
        var today = VisitCounter.Today.ToString("yyyyMMdd");
        var newVisitor = context.Request.Cookies[CookieName] != today;
        if (newVisitor)
            context.Response.Cookies.Append(CookieName, today, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = new DateTimeOffset(DateTime.Today.AddDays(1))
            });
        context.RequestServices.GetRequiredService<VisitCounter>().Hit(newVisitor);
        if (newVisitor) context.Publish(ServiceEvents.VisitDaily, values: new Dictionary<string, string?> { ["ip"] = context.GetUserIP(), ["path"] = context.Request.Path.Value });
    }
}
