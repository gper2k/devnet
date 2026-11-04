# BitPlus.Web.Site.Properties 폴더 개요

## 1. 폴더 역할 및 책임
`BitPlus.Web.Site.Properties`는 애플리케이션의 런타임 호스팅 프로필(`launchSettings.json`)과 사이트 전용 다국어 리소스(`Resources.resx`, `Resources.ko.resx`)를 보관합니다.

---

## 2. 파일 목록 및 구현 상세

| 파일명 | 유형 | 역할 및 세부 내용 |
| :--- | :--- | :--- |
| `launchSettings.json` | JSON | Kestrel 웹 서버 실행 프로필 (HTTP/HTTPS 포트, `ASPNETCORE_ENVIRONMENT=Development` 설정). |
| `Resources.resx` | Resource XML | 기본(영어) 메시지 및 다국어 필드명 정의 (`@UserID`, `@Email`, `@GroupName`, `@Title` 등). |
| `Resources.ko.resx` | Resource XML | 한국어 번역 리소스 정의. |
| `Resources.Designer.cs` | class | Visual Studio ResX 생성기에 의해 생성되는 강력한 형식의 리소스 접근 클래스. |

---

## 3. 코딩 규칙 준수 사항
- JSON 스키마에서 참조하는 모든 `@키명`은 Resources에 대응 키가 반드시 존재해야 함.
- 리소스에는 짧은 문구(라벨·제목·버튼·짧은 메시지)만 둡니다. 한국어 기준 40자 이상인 문장(경로·예시 코드·태그는 빼고 셈: 안내문, 확인 메시지, 메일 기본 본문 등)은 DB 다국어 테이블 시드 `Data/Languages.json`(카테고리 `Text`)에 두고, 화면은 같은 키로 `Lang(key)`(리소스 → DB 순) 조회합니다.
- 예외: 설치 마법사(`/Setup`) 화면·SMTP 테스트 문구(`SetupIntro`, `SetupDatabaseInfo`, `SetupMigrateSlow`, `DbTablePrefixInfo`, `SetupSmtpInfo`, `SetupRedisInfo`, `SmtpTestBody`)는 DB 가 생기기 전에 쓰므로 리소스에 둡니다.
