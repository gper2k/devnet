# BitPlus.Web.Site.Controllers

사이트의 화면(MVC 뷰)을 돌려주는 **뷰 컨트롤러**와 설치 마법사가 있는 폴더입니다. JSON 을 돌려주는 REST API 컨트롤러는 [`API/`](API/README.md) 폴더에 있습니다.

## 구분 규칙

- **뷰 컨트롤러 (이 폴더)**: 전체 페이지나 탭 조각(HTML)을 돌려주며 `ViewControllerBase`(`BitPlus.Web/HTTP/View`)를 상속합니다. 뷰 컨트롤러는 JSON 을 돌려주지 않습니다.
- **API 컨트롤러 (`API/`)**: `APIControllerBase` 를 상속하고 표준 응답 `Result`/`Result<T>` 를 돌려줍니다.
- 권한이 필요한 액션은 `[Auth("정책 이름")]`(예: `WebConfig.ManagerPolicyName`)으로 막습니다. 뷰 요청이 거부되면 401/403 상태 코드만 보내고 `wwwroot/html` 의 오류 페이지가 보입니다.

## 컨트롤러

| 파일 | 주요 경로 | 내용 |
| :--- | :--- | :--- |
| `AccountController.cs` | `/Account/...` | 로그인·회원 가입·아이디/비밀번호 찾기·비밀번호 재설정·메일 인증 화면, 로그아웃 |
| `HomeController.cs` | `/`, `/Service/Color`, `/Service/Icons` | 홈(위젯 배치), 색상표·아이콘 목록 같은 도움 화면 |
| `ContentController.cs` | `/content`, `/content/menu/{메뉴 키}`(메뉴별 포탈), `/content/{게시판}/{view?}`, `/content/{게시판}/view/{글}`, `/content/view/{글 식별키}`, `/content/manage/{게시판}` | 게시판 목록·글 보기·글쓰기·게시판 설정 화면. 형식(일반·갤러리·포럼·메뉴얼·FAQ·설문)별 뷰는 `Views/Content/{형식}` |
| `ViewController.cs` | `/view/{번호}`, `/view/edit/{번호?}` | 관리자가 작성하는 보기 전용 페이지(이용약관 등) 보기와 편집. 번역이 있으면 현재 언어 번역을 보여 줌 |
| `UserController.cs` | `/user`, `/user/notice`, `/user/message`, `/user/posts`, `/user/setting`, `/user/{사용자}` | 내 정보·알림·쪽지·내 글·설정, 다른 사용자 프로필 |
| `SearchController.cs` | `/search` | 통합 검색 화면 (검색 공급자는 `Search/` 폴더) |
| `ManagerController.cs` | `/Manager/...` | 관리자 콘솔 화면(회원·그룹·역할, 약관·메일 템플릿, 게시판·파일 규칙·신고·페이지, 메뉴·팝업·서비스, 시스템 설정·로그 등). 관리자 정책 필요 |
| `SetupController.cs` | `/Setup` | 설치 마법사: DB·SMTP·Redis 연결 테스트, 마이그레이션 실행, 기본 데이터(역할·관리자 메뉴·기본 게시판·기본 페이지) 등록, `appsettings.Setup.json` 작성 |
| `SetupMigrationProgress.cs` | - | 설치 중 마이그레이션 진행률을 알려 주는 EF Core 명령 인터셉터 |

## 새 화면 추가하기

1. 이 폴더에 `ViewControllerBase` 를 상속한 컨트롤러(또는 기존 컨트롤러에 액션)를 만들고 `[HttpGet("경로")]` 를 붙입니다.
2. 뷰는 `Views/{컨트롤러}/{액션}.cshtml` 에 두고, 페이지 전용 스타일은 같은 이름의 `.cshtml.scss`(→ `.cshtml.css`, `compilerconfig.json` 에 등록)로 씁니다.
3. 데이터 저장·조회는 API 컨트롤러(`API/`)에 두고 화면 스크립트에서 `common.getSend/postSend` 로 부릅니다.
4. 화면 문구는 `this.Lang("키")` 로 읽고, 키는 `Properties/Resources*.resx` 에 추가합니다.
