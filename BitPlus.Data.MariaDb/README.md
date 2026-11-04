# BitPlus.Data.MariaDb

MariaDB 10 DB 프로바이더 (`IDbProvider` 구현). 사이트 프로젝트가 참조하면 `DbProviders` 가 자동으로 찾아 설치 화면 목록에 "MariaDB 10" 으로 나타납니다.

- EF Core 프로바이더: `Microting.EntityFrameworkCore.MySql` 10.0.12 (MIT, Pomelo 포크, EF Core 10 지원, MariaDB 공식 지원). 접속 드라이버는 `MySqlConnector` (MIT). SQL 기준 버전은 `MariaDbServerVersion` 10.6 으로 고정해 설계 시점(`dotnet ef`)에도 DB 접속이 필요 없습니다.
- 마이그레이션: 사이트 프로젝트 `Data/Migrations/MariaDb` (`dotnet ef migrations add <이름> -o Data/Migrations/MariaDb -- MariaDb`).

| 파일 | 유형 | 내용 |
| :--- | :--- | :--- |
| `MariaDbProvider.cs` | class | 입력 항목(서버·포트 3306·DB 이름·계정·비밀번호), 접속 문자열(utf8mb4), 접속 테스트(`SELECT VERSION()`, `information_schema.SCHEMATA`), DB 생성(utf8mb4_unicode_ci), EF 연결(`UseMySql` + 마이그레이션 구분 + MariaDB 기록 테이블). |
| `MariaDbDeleteAliasInterceptor.cs` | class | 명령 가로채기. EF Core 일괄 삭제(`ExecuteDeleteAsync`)가 만드는 `DELETE FROM \`표\` AS \`별칭\`` 를 MariaDB 가 받는 `DELETE \`별칭\` FROM \`표\` AS \`별칭\`` 로 바꿈(MySQL 8.0.16+ 전용 문법이라 MariaDB 10 은 문법 오류). `UseProvider` 가 등록. |
| `MariaDbHistoryRepository.cs` | class | 마이그레이션 기록 테이블 처리. MariaDB 는 `GET_LOCK(이름, -1)`(무제한 대기)에 NULL 을 돌려 실패하므로 양수 대기 시간(3600초)으로 잠금. |
| `MariaDbMigrationsLock.cs` | class | 마이그레이션 잠금 해제(`RELEASE_LOCK`). |
