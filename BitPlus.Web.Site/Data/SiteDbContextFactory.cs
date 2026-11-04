using BitPlus.Data.Provider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// EF Core 마이그레이션 생성용(dotnet ef) 디자인 타임 DbContext 팩토리. 실제 DB 에는 접속하지 않습니다.
/// 테이블 이름 접두사는 실제 값 대신 <see cref="MigrationPrefix.Token"/> 으로 만들어 마이그레이션에 고정되지 않게 하고(실행 시 치환),
/// 프로바이더는 인자(-- SqlServer)로 고릅니다(기본: 등록된 첫 프로바이더).
/// <para>예: dotnet ef migrations add InitialCreate -o Data/Migrations/SqlServer</para>
/// </summary>
public class SiteDbContextFactory : IDesignTimeDbContextFactory<SiteDbContext>
{
    /// <summary>
    /// <see cref="SiteDbContextFactory"/> 기본 생성자.
    /// </summary>
    public SiteDbContextFactory() { }

    /// <summary>
    /// 마이그레이션 생성용 DbContext 를 만듭니다.
    /// </summary>
    /// <param name="args">dotnet ef 추가 인자 (첫 값 = 프로바이더 이름)</param>
    /// <returns>DbContext</returns>
    public SiteDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(config).BuildServiceProvider();
        var provider = DbProviders.Get(args.FirstOrDefault()) ?? DbProviders.All[0];

        var options = new DbContextOptionsBuilder<SiteDbContext>();
        WebAppBuilderExtend.BindDbOption(services, options, o => provider.UseProvider(o, "Server=design;Database=design", MigrationPrefix.Token), MigrationPrefix.Token);
        return new SiteDbContext(options.Options);
    }
}
