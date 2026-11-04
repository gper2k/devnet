using BitPlus.Data.CommonModel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// 기본 다국어 문장 시드: 긴 안내 문장·메일 기본 본문 등 리소스 파일에 두지 않는 문장(<c>Data/Languages.json</c>, { 키: { 문화권: 값 } })을 다국어 테이블에 넣습니다.
/// 화면은 키로 조회(<c>Lang(key)</c>: 리소스 → DB)하므로 키만 맞으면 됩니다. 이미 있는 (카테고리, 키, 문화권) 행은 관리자가 고친 값일 수 있어 건드리지 않습니다.
/// </summary>
public static class LanguageSeed
{
    /// <summary>시드 문장의 다국어 카테고리</summary>
    public const string Category = "Text";

    /// <summary>시드 파일 경로 (콘텐츠 루트 기준)</summary>
    public const string FilePath = "Data/Languages.json";

    /// <summary>
    /// 시드 파일에서 없는 행만 다국어 테이블에 추가하고 저장합니다. 파일이 없으면 아무것도 하지 않습니다.
    /// </summary>
    /// <param name="db">사이트 DB 컨텍스트</param>
    /// <param name="contentRoot">콘텐츠 루트 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추가한 행 수</returns>
    public static async Task<int> SyncAsync(SiteDbContext db, string contentRoot, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(contentRoot, FilePath);
        if (!File.Exists(file)) return 0;

        await using var stream = File.OpenRead(file);
        var items = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, string>>>(stream, cancellationToken: cancellationToken) ?? [];
        var saved = (await db.Languages.AsNoTracking().Where(p => p.Category == Category).Select(p => new { p.Key, p.Culture }).ToListAsync(cancellationToken))
            .Select(p => (p.Key, p.Culture)).ToHashSet();

        var added = 0;
        foreach (var (key, values) in items)
            foreach (var (culture, text) in values)
                if (!saved.Contains((key, culture)))
                {
                    db.Languages.Add(new LanguageData(key, culture, text, Category));
                    added++;
                }
        if (added > 0) await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    /// <summary>
    /// 사이트 시작 때 기본 다국어 문장을 맞춥니다 (설치 후에만). 실패해도 사이트는 시작합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    public static async Task SyncLanguagesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            await SyncAsync(scope.ServiceProvider.GetRequiredService<SiteDbContext>(), app.Environment.ContentRootPath);
        }
        catch (Exception ex)
        {
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Languages").LogError(ex, "Language seed sync failed");
        }
    }
}
