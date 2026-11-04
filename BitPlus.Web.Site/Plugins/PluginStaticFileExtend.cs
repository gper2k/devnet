using Microsoft.AspNetCore.Builder;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 플러그인 내장 정적 리소스(wwwroot)를 포함한 정적 파일 파이프라인 구성 확장 메서드입니다.
/// </summary>
public static class PluginStaticFileExtend
{
    /// <summary>
    /// 플러그인 내장 정적 리소스(wwwroot)를 포함하여 브라우저 캐시 헤더와 함께 제공합니다.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/> 인스턴스</param>
    /// <param name="pluginManager">플러그인 매니저</param>
    /// <returns>체이닝을 위한 <see cref="WebApplication"/></returns>
    public static WebApplication UsePluginStaticFiles(this WebApplication app, PluginManager pluginManager)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(pluginManager);
        var compositeProvider = pluginManager.GetCompositeFileProvider(app.Environment.WebRootFileProvider);
        var libCache = App.DevMode ? "no-cache" : "public, max-age=604800";
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = compositeProvider,
            OnPrepareResponse = context =>
            {
                var request = context.Context.Request;
                if (request.Query.ContainsKey("v")) context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                else if (request.Path.StartsWithSegments("/lib")) context.Context.Response.Headers.CacheControl = libCache;
            }
        });
        return app;
    }
}
