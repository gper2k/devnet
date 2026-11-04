using BitPlus.Web.Site.Data;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯 기능 등록·시작 처리: 페이지 영역 읽기 서비스와 위젯 데이터 로더 등록, 사이트 시작 때 위젯·영역 정보 동기화.
/// </summary>
public static class WidgetServiceExtend
{
    /// <summary>
    /// <see cref="PageSectionReader"/> 와 사이트 어셈블리의 <see cref="IWidgetLoader"/> 구현을 요청 단위로 등록합니다 (DB 를 쓰므로 설치 후에만).
    /// ponytail: 사이트 어셈블리만 찾음 — 플러그인 DLL 위젯 로더는 플러그인을 읽는 AddServiceManager 쪽으로 옮길 때 추가.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    public static IServiceCollection AddWidgets(this IServiceCollection services)
    {
        services.AddScoped<PageSectionReader>();
        IEnumerable<System.Reflection.Assembly> assemblies;
        try { assemblies = Plugins.PluginManager.Instance.AllAssemblies; }
        catch { assemblies = [typeof(WidgetServiceExtend).Assembly]; }

        foreach (var asm in assemblies)
        {
            try
            {
                foreach (var type in asm.GetTypes().Where(p => p.IsClass && !p.IsAbstract && typeof(IWidgetLoader).IsAssignableFrom(p)))
                    services.AddScoped(typeof(IWidgetLoader), type);
            }
            catch { }
        }
        return services;
    }

    /// <summary>
    /// 위젯·영역 폴더 JSON 을 정보 테이블과 맞춥니다 (사이트 시작 때 한 번, 설치 후에만). 실패해도 사이트는 시작합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    public static async Task SyncWidgetsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Widgets");
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<SiteDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<WebConfig>();
            await WidgetCatalog.SyncAsync(db, app.Environment.ContentRootPath, logger);
            await PageLayouts.SyncAsync(db, config, app.Environment.ContentRootPath, logger);
            await SiteSeed.SyncSamplePostsAsync(db, app.Environment.ContentRootPath, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Widget catalog sync failed");
        }
    }
}
