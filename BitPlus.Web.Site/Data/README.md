# BitPlus.Web.Site.Data 폴더 개요

## 1. 폴더 역할 및 책임
`BitPlus.Web.Site.Data`는 이 웹 사이트 구현체 전용 **데이터베이스 컨텍스트(`SiteDbContext`) 및 사이트 맞춤 엔티티**들을 정의합니다.
`BitPlus.Web`의 `WebDbContext<SiteUser>`를 상속받으며, `BitPlus.Board`의 게시판 엔티티들을 통합 관리하여 SQL Server 데이터베이스와의 모든 상호작용을 총괄합니다.

---

## 2. 파일 목록 및 구현 상세

| 파일명 | 유형 | 역할 및 세부 내용 |
| :--- | :--- | :--- |
| `SiteDbContext.cs` | class | 사이트 최종 DbContext.<br>• `WebDbContext<SiteUser>` 상속 및 `IBoardDbContext` 구현.<br>• `DbSet<BoardConfig> BoardConfigs { get; set; }`<br>• `DbSet<BoardCategory> BoardCategories { get; set; }`<br>• `DbSet<BoardPost> BoardPosts { get; set; }`<br>• `DbSet<BoardComment> BoardComments { get; set; }`<br>• `DbSet<BoardAttachment> BoardAttachments { get; set; }`<br>• `OnModelCreating`: 게시판 복합 인덱스(BoardKey, CategoryId, CreatedAt) 및 외래키 캐스케이드 규칙 구성. |
| `SiteDbContextFactory.cs` | class | `dotnet ef` 용 디자인 타임 DbContext 팩토리. appsettings.json 의 `DbBuild`(테이블 Prefix)를 앱과 같게 적용, 프로바이더는 인자(기본 SqlServer), 실제 DB 에는 접속하지 않음. |
| `Migrations/SqlServer/` | folder | SQL Server 용 EF 마이그레이션 (`InitialCreate`: 44개 테이블). |
| `DbMigrator.cs` | static class | 설치된 사이트(`Created` 있음)의 시작 시 대기 마이그레이션 자동 적용(`app.MigrateDatabaseAsync()`). 기록에 `InitialCreate` 가 없는 DB 는 적용하지 않고 오류 로그, 실패해도 로그만 남기고 시작. 동시 시작은 EF 마이그레이션 잠금(SQL Server applock, `MariaDbMigrationsLock`)으로 한 곳만 적용. |
| `LanguageSeed.cs` | static class | 기본 다국어 문장 시드: `Languages.json` 을 다국어 테이블(카테고리 `Text`)에 넣음 — 없는 (키, 문화권) 행만 추가하고 관리자가 고친 행은 그대로. 설치 seed 단계와 사이트 시작(`SyncLanguagesAsync`, 설치 후) 때 실행. |
| `SiteSeed.cs` | static class | 사이트 기본 구성 시드(설치 seed 단계, 보기 전용이 아닌 게시판이 없을 때만): `SiteSeed.json` 의 기본 게시판(설정·옵션·첨부 규칙·역할 접근 권한·게시판 메뉴 `Content_{번호}`)과 샘플 글(작성자 없음, 작성일 = 지금 - daysAgo, children = 포럼 답글·설명서 하위 문서), 사용자 메뉴, 위젯 페이지 기본 배치의 게시판 별칭(`"@별칭"`)을 번호로 바꿔 배치 설정 `PageLayout.{페이지}` 로 저장. `NewRuleAsync` = 컨텐츠 규칙 번호 규칙(설치 공용). |
| `SiteSeed.json` | 시드 파일 | `boards`[alias, type, title{문화권}, category, options, access[{mode, role}], file{extensions, count, size, totalSize, access}, menu{parent(`ContentMap` 또는 `@별칭`), icon, sort}, posts[{title, category, notice, daysAgo, body, children}]], `menus`[{key, parent, names, sort, action(`{별칭}` = 게시판 번호)}]. 샘플 이미지는 본문에서 `/img/sample/…` 참조(첨부 없음). |
| `Languages.json` | JSON | 기본 다국어 문장 { 키: { 문화권: 값 } } — 리소스에 두지 않는 40자 이상 문장(안내문, 확인 메시지, 메일 기본 본문 등). 화면은 같은 키로 `Lang(key)`(리소스 → DB) 조회. |
| `SystemNames.cs` | class | 설치 시 등록하는 시스템 역할·그룹 이름 상수(SystemManager/Manager/AccountManager/ContentManager/Member 역할, Manager/AccountManager/ContentManager/Member 그룹). 설치 seed 와 첫 가입자 처리 전용 — 권한 판단은 정책(`Policies`)이 함. |
| `SiteUser.cs` | class | 사이트 확장 사용자 엔티티.<br>• `AppUser` 상속.<br>• 사이트 고유의 추가 속성(예: 프로필 이미지 경로, 닉네임, 최종 로그인 시각) 확장 지원. |

---

## 3. 데이터베이스 생성과 기본 데이터
- 시작 시 자동 초기화는 없습니다. DB 생성·테이블 생성·기본 데이터는 설치 마법사(`/Setup`, `Controllers/SetupController.cs`)가 처리합니다.
- 테이블은 EF Core 마이그레이션으로 만듭니다: `Migrations/SqlServer`(SQL Server 용). 다른 DB 를 지원하면 프로바이더별 마이그레이션을 따로 만듭니다.
- 마이그레이션 추가: `dotnet ef migrations add <이름> -o Data/Migrations/SqlServer` (BitPlus.Web.Site 폴더에서, `SiteDbContextFactory` 사용).
- 기본 데이터(설치 seed 단계): 시스템 역할 `SystemManager`(최종 관리자, 첫 가입자에게 부여)·`Manager`(일반 관리자, 관리자 표시 겸 관리자 메뉴 접근 역할, 첫 가입자는 SystemManager 와 함께 가짐)·`Member`(일반, 가입 기본 역할), 시스템 그룹 `Member`(가입 기본 그룹), 계정 규칙(가입 기본 역할·그룹 Member), 관리자 시스템 메뉴(영문 이름, 삭제 불가), 보기 전용 컨텐츠 규칙 "PageView"(`OnlyView`), 인증·암호화 마스터 키.

---

## 4. 코딩 규칙 준수 사항
- 파일당 하나의 클래스만 선언 (`One Type Per File`).
- 생성자에서 필드 초기화 및 `this.` 명시.
- 1줄 단문 제어문 인라인 처리.
