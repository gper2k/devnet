using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// 설치된 사이트의 DB 스키마 업그레이드: 시작할 때 아직 적용하지 않은 마이그레이션(<c>Data/Migrations/{프로바이더}</c>)을 적용합니다.
/// 여러 서버가 함께 시작해도 마이그레이션 잠금(SQL Server sp_getapplock, MariaDB <c>MariaDbMigrationsLock</c>)으로 한 곳만 적용합니다.
/// </summary>
public static class DbMigrator
{
    /// <summary>전체 스키마를 만드는 첫 마이그레이션 이름 (설치 마법사가 적용)</summary>
    private const string InitialName = "_InitialCreate";

    /// <summary>
    /// 대기 중인 마이그레이션을 적용합니다 (없으면 아무것도 하지 않음). 실패하면 오류를 로그에 남기고 사이트는 그대로 시작합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션</param>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<SiteDbContext>().Database;
            var pending = (await db.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0) return;
            if (pending.Any(p => p.EndsWith(InitialName, StringComparison.Ordinal))) // 설치된 DB 인데 기록에 InitialCreate 가 없으면 이전 개발 단계의 다른 ID 로 설치된 DB — 전체 생성을 다시 돌리면 실패하므로 적용하지 않음
            {
                logger.LogError("Migration history does not contain {Initial}; pending migrations were not applied: {Migrations}", InitialName, string.Join(", ", pending));
                return;
            }
            logger.LogInformation("Applying migrations: {Migrations}", string.Join(", ", pending));
            db.SetCommandTimeout(600); // MariaDB 는 DDL 이 느림 (설치 마법사와 같은 값)
            await db.MigrateAsync();
            logger.LogInformation("Migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed");
        }
    }
}
