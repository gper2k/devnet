using BitPlus.Data;
using BitPlus.Web.Data;
using BitPlus.Web.Site.Data.Widget;
using Microsoft.EntityFrameworkCore;

namespace BitPlus.Web.Site.Data;

/// <summary>
/// 웹 사이트 전용 데이터베이스 컨텍스트 클래스입니다.
/// <see cref="WebDbContext"/>를 상속받아 계정, 메뉴, 정책 및 사이트 고유의 엔티티(위젯·영역 종류)를 관리합니다.
/// </summary>
public class SiteDbContext : WebDbContext
{
    /// <summary>
    /// <see cref="SiteDbContext"/> 매개변수가 없는 기본 생성자.
    /// </summary>
    public SiteDbContext() : base() { }

    /// <summary>
    /// <see cref="SiteDbContext"/> DbContext 옵션을 전달받는 생성자.
    /// </summary>
    /// <param name="options">DbContext 옵션</param>
    public SiteDbContext(DbContextOptions options) : base(options) { }

    /// <summary>위젯 종류 정보 (Views/Widgets 의 widget.json 에서 동기화)</summary>
    public DbSet<WidgetType> WidgetTypes { get; set; }

    /// <summary>영역 종류 정보 (Views/Sections 의 section.json 에서 동기화)</summary>
    public DbSet<SectionType> SectionTypes { get; set; }

    /// <summary>
    /// EF Core Fluent API 모델 구성 — 공통 모델에 위젯·영역 종류 테이블을 더합니다.
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder 인스턴스</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var prefix = this.GetPrefix();
        WidgetType.Configure(modelBuilder.Entity<WidgetType>(), prefix);
        SectionType.Configure(modelBuilder.Entity<SectionType>(), prefix);
    }
}
