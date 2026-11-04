# Data/Migrations

EF Core 마이그레이션. DB 종류(프로바이더)마다 하위 폴더를 두고, 각 폴더에는 전체 스키마를 만드는 `InitialCreate` 하나만 있습니다.

| 폴더 | 내용 |
| :--- | :--- |
| `SqlServer/` | SQL Server 용 (`DbProviders` 의 `SqlServer`). `InitialCreate` = 전체 스키마. 스냅숏 `SiteDbContextSqlServerModelSnapshot`. |
| `MariaDb/` | MariaDB 용 (`BitPlus.Data.MariaDb`). `InitialCreate` = 전체 스키마. 스냅숏 `SiteDbContextMariaDbModelSnapshot`. |

- 같은 어셈블리의 마이그레이션은 `ProviderMigrationsAssembly` 가 네임스페이스(`Data.Migrations.<프로바이더>`)로 구분합니다.
- 모델 스냅숏 클래스 이름이 DB 별로 같으면 `dotnet ef` 가 다른 DB 의 스냅숏 파일을 덮어쓰므로 `SiteDbContext{프로바이더}ModelSnapshot` 으로 구분해 둡니다.
- 적용: 새 설치는 설치 마법사의 migrate 단계(`Database.MigrateAsync`). 설치된 사이트는 시작할 때 `DbMigrator.MigrateDatabaseAsync` 가 대기 중인 마이그레이션을 적용합니다(기록 테이블 `{접두사}_EFMigrationsHistory` 에 `InitialCreate` 가 없으면 적용하지 않고 오류 로그). **운영 DB 에 반영하기 전에는 반드시 DB 를 백업하세요.**
- 테이블 이름 접두사는 마이그레이션에 고정하지 않습니다. `SiteDbContextFactory` 가 접두사 토큰(`MigrationPrefix.Token`, `BPXPREFIX`)으로 만들고, 실행 시 프로바이더의 SQL 생성기가 설정 접두사(`DbBuild:Prefix`, 설치 때 입력)로 바꿉니다. 마이그레이션 기록 테이블도 `{접두사}_EFMigrationsHistory` 입니다. 마이그레이션 안의 표 이름은 손으로 고치지 않습니다.

## 다른 DB 프로바이더 추가
1. `IDbProvider` 구현 프로젝트를 만들고(`BitPlus.Data.MariaDb` 참고) 사이트 프로젝트에서 참조합니다.
2. BitPlus.Web.Site 폴더에서 `dotnet ef migrations add InitialCreate -o Data/Migrations/<프로바이더> -- <프로바이더>`
3. 새로 생긴 `SiteDbContextModelSnapshot` 클래스/파일 이름을 `SiteDbContext{프로바이더}ModelSnapshot` 으로 바꿉니다.
4. `dotnet ef migrations has-pending-model-changes -- <프로바이더>` 로 모델과 일치하는지 확인합니다.

## 모델 변경 (설치한 DB 가 있을 때)
기존 마이그레이션을 고치지 말고 프로바이더마다 새 마이그레이션을 추가합니다(`dotnet ef migrations add <이름> -o Data/Migrations/<프로바이더> -- <프로바이더>`). 기존 칸 변경·삭제 대신 칸 추가(nullable 또는 기본값)로 기존 데이터를 유지하고, 추가 뒤 모든 프로바이더를 `has-pending-model-changes` 로 확인합니다.
