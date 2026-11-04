# BitPlus.Web.Site

실제로 실행되는 웹사이트(호스트) 프로젝트입니다. MVC 뷰(화면)와 REST API 를 함께 제공하며, `BitPlus.Web`(ASP.NET Core 확장)과 `BitPlus`(코어)를 기반으로 동작합니다.

- 대상 프레임워크: `net10.0` (`Microsoft.NET.Sdk.Web`)
- 주요 패키지: `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`(개발 환경 API 문서), `QRCoder`, `Microsoft.EntityFrameworkCore.Design`(마이그레이션 도구)
- DB 프로바이더: SQL Server(기본 포함), MariaDB(`BitPlus.Data.MariaDb` 참조, 자동 인식, MIT 라이선스 `Microting.EntityFrameworkCore.MySql`)

## 실행과 설치

```bash
dotnet run --project BitPlus.Web.Site
```

- 설치 전에는 모든 요청이 설치 마법사(`/Setup`)로 갑니다. 마법사가 DB 를 만들고 `appsettings.Setup.json` 을 써서 사이트를 다시 시작합니다.
- `setup.json` 을 사이트 폴더에 두면 마법사 입력값을 미리 채웁니다. 두 파일 모두 접속 정보가 들어가므로 저장소에 올리지 마세요.
- 설치된 사이트는 시작할 때 아직 적용하지 않은 DB 마이그레이션을 적용하고(`Data/DbMigrator.cs`), 위젯 목록과 기본 다국어 문장을 맞춥니다.
- 개발 환경에서는 `/scalar` 에서 API 문서를 볼 수 있습니다(OpenAPI 규격은 `/openapi/v1.json`).

### 큰 첨부 파일과 리버스 프록시

첨부 파일은 5MB 이하면 한 번에, 그보다 크면(최대 2GB) 5MB 조각으로 나눠 올립니다(`api/content/file/chunk/*`). 그래서 서버 전체의 요청 크기 한도(Kestrel 기본 약 28.6MB)는 올리지 않아도 됩니다. 실제로 받을 수 있는 크기는 게시판 설정의 파일 규칙(파일당 용량·전체 용량)이 정합니다.

앞에 nginx 같은 리버스 프록시를 두면 조각 하나(5MB + 폼 여유)가 지나갈 수 있게 요청 크기 한도를 6MB 이상으로 열어 주세요. nginx 기본값은 1MB 입니다.

```nginx
client_max_body_size 8m;
proxy_request_buffering off;   # 조각을 프록시에 모아 두지 않고 바로 넘김 (선택)
```

임시 조각은 업로드 폴더의 `_chunks` 에 쌓이고, 끝내지 않은 업로드는 36시간 뒤 자동으로 지워집니다. 업로드 폴더가 있는 디스크에 올릴 파일 크기 + 100MB 이상이 비어 있어야 업로드를 시작할 수 있습니다.

## 폴더 구성

| 폴더 | 내용 |
| :--- | :--- |
| `Controllers/` | 뷰 컨트롤러(화면)와 설치 마법사. JSON API 는 `Controllers/API/` |
| `Views/` | Razor 뷰. 레이아웃은 `Views/Layouts/{General,Account}/{이름}`, 공용 조각은 `Views/Shared` |
| `Models/` | 뷰 모델 |
| `Data/` | `SiteDbContext`, 마이그레이션(`Migrations/SqlServer`, `Migrations/MariaDb`), 설치 기본 데이터(`SiteSeed.json`, `Languages.json`) |
| `Services/` | 백그라운드 서비스(컨텐츠 AI, 오류 알림 메일 등) — 관리자 콘솔 "서비스"에서 켜고 설정 |
| `Ai/` | AI 접속(OpenAI 호환·Claude·Gemini) 호출 클라이언트 |
| `Search/` | 통합 검색 공급자(게시판·페이지 등) |
| `Widgets/` | 홈·포털 위젯 |
| `OAuth/` | 외부 로그인 공급자 구성 |
| `Middlewares/` | 사이트 전용 미들웨어(설치 안내, 방문자 집계 등) |
| `ValidConfigs/` | `[Valid("...")]` 가 쓰는 JSON 입력 검증 규칙 |
| `Properties/` | 화면 문구 리소스(`Resources.resx` 영어 기본, `Resources.ko.resx` 한국어), 실행 프로필 |
| `wwwroot/` | 정적 파일: `ts`(TypeScript 원본) → `js`, `css`, `lib`(에디터·아이콘 등), `img`, `html`(오류 페이지·기본 페이지 원본), `dev`(관리자 DevKit 견본 화면) |

## 라이선스

- `LICENSE.txt`: 이 프로젝트의 MIT 라이선스 (설치 마법사 첫 화면에 표시, 게시 포함).
- `THIRD-PARTY-NOTICES.txt`: 사용한 NuGet 패키지·`wwwroot/lib` 라이브러리의 저작권 고지와 라이선스 전문 (게시 포함). 패키지를 추가·변경하면 함께 고칩니다. GPL 등 copyleft 라이선스 패키지는 쓰지 않습니다.

실행 중에 생기는 `Logs/`(JSON 로그)와 `Uploads/`(업로드 파일·방문 집계)는 저장소에 넣지 않습니다.

## 요청 파이프라인 (`Program.cs`)

순서가 중요합니다. 바꾸기 전에 각 단계가 앞 단계에 무엇을 기대하는지 확인하세요.

1. 설정 읽기(`appsettings.json` → `appsettings.Setup.json` → 환경 변수·명령줄), 설치 여부 판단
2. 서비스 등록: MVC(가장 먼저) → 로거·CORS·응답 압축 → DB(`SiteDbContext`, 선택한 프로바이더) → Identity → 정책 인가 → 다국어 리소스 → OpenAPI → 방문자 집계 → (설치 후) 서비스 매니저·위젯·외부 로그인
3. 시작 작업(설치 후): 마이그레이션 적용 → 마스터 키 로드 → 위젯·다국어 문장 동기화
4. 미들웨어: 오류 페이지 → 오류 로깅 → CORS → OpenAPI(개발: Scalar, 운영: HTTPS) → 응답 압축 → 정적 파일 → 라우팅 → **문화권 결정** → 설치 안내 → 방문자 집계 → **계정 토큰(쿠키)** → 인증 → 인가 → 컨트롤러

## 확장할 때

- 새 화면: [`Controllers/README.md`](Controllers/README.md) 의 "새 화면 추가하기"
- 새 API: `Controllers/API/` 에 `APIControllerBase` 상속 컨트롤러, 입력은 요청 DTO 대신 `[FromForm]` 개별 필드, 응답은 `Result`
- 모델(테이블) 변경: [`Data/Migrations/README.md`](Data/Migrations/README.md) 대로 DB 마다 마이그레이션 추가
- 화면 문구: `Properties/Resources*.resx` 에 키를 추가(두 파일 모두)하고 뷰에서 `this.Lang("키")`
