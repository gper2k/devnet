# BitPlus.Web.Site/Controllers/API

JSON 을 돌려주는 **API 컨트롤러** 폴더. 모두 `BitPlus.Web.HTTP.API.APIControllerBase`(`[ApiController]`, `api/[controller]` 라우트, `Complete()/Error()` 등 Result 응답)를 상속합니다. 네임스페이스 `BitPlus.Web.Site.Controllers.API`.

- 요청은 DTO 클래스 없이 개별 `[FromForm]` 필드, 사전은 JSON 문자열 필드, 목록은 반복 필드.
- BitPlus.Web 의 메시지 리소스는 `using WebResources = BitPlus.Web.Properties.Resources;` 별칭으로 참조.

| 파일 | 라우트 | 내용 |
| --- | --- | --- |
| `AccountController.cs` | `api/account` | **사용자 전용 계정/인증**: 로그인(`POST signin`), 로그아웃(`POST signout`), 가입 약관 목록(`GET terms`), 회원 가입(`POST join`), 인증 메일 재발송(`POST mail/resend`), 인증 완료 확인(`POST/GET mail/check`), 아이디 찾기 메일 발송(`POST find/id`), 비밀번호 재설정 메일 발송(`POST find/password`, `POST password/reset-request`), 비밀번호 재설정(`POST password/reset`). 공통 헬퍼 `ProcessSignInResultAsync`로 메일 검증 모드(On/Warning/Off)에 따른 세션 발급 및 분기 로직 완전 일원화 |
| `MembershipController.cs` | `api/membership` | **관리자 전용 멤버십/조직 관리**: 그룹(group/tree, group/{id}, member/list), 역할(role/list, role/{id}), 권한 정책(policy/list = 구성 파일·DB 병합 목록(source: Config/Db/Override, applied: 적용 쪽), policy/item?name= 구성 파일(읽기 전용)·DB 값, DB 정책 등록/수정/활성 토글/삭제 — 저장마다 정책 캐시 무효화, 관리자 정책을 바꿀 때 현재 사용자가 관리 권한을 잃으면 거부; 구성 파일 정책은 같은 이름의 DB 정책으로 덮어씀, `MembershipController.Policy.cs`), 사용자(user/list, user/{id}), 가입 약관 설정 관리(terms/list, terms/{key}, terms/order), 메일 템플릿 관리(template/{key}, `TemplateItems`: AccountFindId, AccountPwChange, AccountMailConfirm), 계정 구성 관리(rule). 사용자 가입/인증 로직은 AccountController로 전면 이관 |
| `ConfigController.cs` | `api/config` | 관리자 시스템 설정: SMTP 조회/저장/테스트(smtp, smtp/test). 설정 탭이 늘면 여기에 액션 추가 |
| `SiteMapController.cs` | `api/sitemap` | 관리자 사이트 메뉴: 트리(`GET menu/tree`), 하위 목록(`GET menu/list?parentKey=`), 상세(`GET menu/item/{key}`), 등록(`POST menu`), 수정·이동(`PUT menu/item/{key}`, 상위 변경 시 하위 경로 갱신·순환 거부), 사용 전환(`PATCH menu/item/{key}/enabler`), 형제 순서(`PUT menu/order`, parentKey + keys 반복 필드), 삭제(`DELETE menu/item/{key}`, 하위 포함). 식별키 영문/숫자/_-. 1~64자 |
| `LogController.cs` | `api/log` | 관리자 로그 보기(관리자 정책): 종류·날짜 목록(`GET files`), 조건(시간 범위·수준·검색어)에 맞는 항목을 바이트 위치부터 조각으로 읽기(`GET entries`), 원본 JSON 상세(`GET entry`). 로그 제외 규칙 조회(`GET exclude`, 현재 규칙 + 기본 규칙)·저장(`PUT exclude`, `rules` = JSON 배열 `[{category, maxLevel, contains}]`, 최대 100개, 설정 `LogExclude` 에 저장하고 바로 적용) |
| `ContentController.cs` (+ `ContentController.Post.cs`) | `api/content` | 관리자 "컨텐츠 관리" 화면 제거로 규칙 목록/상세/등록/수정/활성/삭제 API(`rule/*`)를 없앴고, 규칙은 형식별 설정 API(`default/*`, 규칙 설정 화면 `/content/manage`)로 다룸. 관리자 콘솔 게시판 목록은 `GET rules`(보기 전용 제외·비활성 포함, 검색어=다국어 제목, 관리자·컨텐츠 관리자 정책만). 파일 규칙 목록/상세/등록/수정/삭제(`filerule/*`, 시스템 규칙 수정·삭제 불가, 시스템 설정의 파일 규칙 탭). 옛 규칙 API 입력(참고): `title`(다국어 JSON), `type`, `fileRuleId`, `category`("key:value;" 문자열, 키 생략 시 값이 키 겸용 → JSON 사전으로 저장), `flags`/`approvers`(반복 필드, 승인자는 전달 순서 = 승인 순서 SortNo), `accessRules`(JSON 배열 [{mode,type,key}]). 글 첨부: 설정 `GET file`, 업로드 `POST file`(5MB 이하 한 번에), 큰 파일(최대 2GB)은 조각 업로드 `POST file/chunk/start`(fileName·fileSize, 권한·규칙·디스크 검사 → uploadId·chunkSize) → `POST file/chunk`(uploadId·offset·chunk, 요청 한도 조각 + 1MB, 응답 received 부터 이어 보냄) → `POST file/chunk/complete`(uploadId, 다시 검사 후 저장) / `POST file/chunk/cancel`, 삭제 `POST file/delete`. 클라이언트는 `common.uploadFile()` 이 크기에 따라 골라 보냄 |

뷰(전체 페이지·조각 HTML)를 돌려주는 컨트롤러는 상위 `Controllers` 폴더에 둡니다.
