using BitPlus;

namespace BitPlus.Web.Site.Middlewares;

/// <summary>
/// 설치(Setup) 진입 제어 미들웨어 등록 확장.
/// </summary>
public static class SetupGuardExtend
{
    /// <summary>설치 마법사 경로 (화면과 API 모두 이 경로 아래)</summary>
    public const string SetupPath = "/Setup";

    /// <summary>
    /// 설치 전에는 설치 마법사 외 요청을 막고(GET 은 /Setup 으로 이동, 그 외는 503 Result), 설치 후에는 /Setup 경로를 404 로 막습니다.
    /// 정적 파일(UseStaticFiles)보다 뒤에 등록합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    /// <param name="installed">설치 여부 (시작 시 판단한 값)</param>
    /// <returns>체이닝용 앱</returns>
    public static IApplicationBuilder UseSetupGuard(this IApplicationBuilder app, bool installed) => app.Use(async (context, next) =>
    {
        var isSetup = context.Request.Path.StartsWithSegments(SetupPath, StringComparison.OrdinalIgnoreCase);
        if (installed == !isSetup)
        {
            await next(context);
            return;
        }

        if (installed)
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        else if (HttpMethods.IsGet(context.Request.Method))
            context.Response.Redirect(SetupPath);
        else
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(Result.Fail("Setup required."));
        }
    });
}
