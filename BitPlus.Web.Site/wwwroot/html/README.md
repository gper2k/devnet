# BitPlus.Web.Site wwwroot/html (오류 페이지)

상태 코드별 오류 페이지(정적 HTML)와 공통 스타일/스크립트. 사이트 CSS 와 별개로 독립 동작하며(아이콘만 `/lib/fluenticons` 사용), 어느 환경에서나 쓸 수 있도록 **문구는 영어 한 가지**로만 작성합니다(언어 선택 없음). 밝은/어두운 테마를 따릅니다.

## 어떻게 보여 주나

`Program.cs` 의 `app.UseErrorPages()` (`BitPlus.Web/WebAppExtend.cs`, `HTTP/ErrorPageWriter.cs`):
- 본문 없는 4xx/5xx 응답(권한 없음, 없는 주소 등)은 `/html/{상태코드}.html` 로 응답합니다 (주소는 그대로, 상태 코드도 그대로).
- 처리되지 않은 예외는 `500.html` (개발 모드는 개발자 예외 페이지).
- 해당 코드의 파일이 없으면 5xx 는 `500.html`, 그 외 4xx 는 `400.html` 로 대신합니다.
- API 요청(`/api` 경로, 또는 JSON 만 받는 요청)은 HTML 대신 `Result` 실패 JSON(`{ success:false, message, data:{ErrorCode} }`)과 상태 코드를 돌려줍니다.
- `[Auth]` 필터가 뷰 요청을 거부하면 401/403 상태만 보내고 이 페이지가 보여 줍니다.

## 파일

| 파일 | 상태 | 상황 | 색조 |
| :--- | :--- | :--- | :--- |
| `400.html` | 400 | 잘못된 요청 (주소·입력 확인) | warning |
| `401.html` | 401 | 로그인 필요 (로그인 버튼, 오류 난 주소로 `returnUrl` 이어줌) | info |
| `403.html` | 403 | 접근 권한 없음 (관리자에게 권한 요청 안내) | danger |
| `404.html` | 404 | 페이지 없음 (주소 확인) | info |
| `405.html` | 405 | 허용되지 않는 요청 방식 | warning |
| `408.html` | 408 | 요청 시간 초과 (다시 시도) | warning |
| `429.html` | 429 | 요청이 너무 많음 (잠시 후 다시) | warning |
| `500.html` | 500 | 서버 오류 — 일반 서버 오류 페이지, 5xx 의 대체 페이지 | danger |
| `503.html` | 503 | 서비스 일시 사용 불가 (점검·과부하) | warning |
| `error.css` | - | 공통 스타일: 상태 코드 큰 글자 + 가운데 숫자 자리에 상황 아이콘, 색조는 body 클래스(`info`/`warning`/`danger`)로 | - |
| `error.js` | - | 버튼 동작(이전/다시 시도/로그인 `returnUrl`) | - |

모든 페이지는 같은 구조입니다: `ERROR 코드 · 영문 이름` 라벨 → 큰 상태 코드(가운데 숫자 = 상황 아이콘) → 제목 → 설명 → 버튼(첫 버튼이 주 동작: 이전 페이지 / 다시 시도 / 로그인, 홈으로).

새 상태 코드 추가: 기존 페이지를 복사해 숫자·아이콘(`fi…`)·라벨·영문 문구·`body` 색조 클래스만 바꾸면 됩니다.

## 기본 보기 전용 페이지 원본 (설치 시드)

설치 마법사(`SetupController.NewPageAsync`)가 보기 전용(PageView) 규칙에 등록하는 기본 페이지 본문입니다. `{이름}.ko.html` 이 원문(글의 원문 언어 `ko`), `{이름}.en.html` 이 영문 번역(사람 번역, `ContentTranslations`)으로 함께 들어갑니다. 이미 설치된 사이트에는 영향이 없습니다(새 설치만).

| 파일 | 페이지 |
| :--- | :--- |
| `Welcome.ko.html` / `Welcome.en.html` | 환영 페이지 (/view/1) |
| `Terms.ko.html` / `Terms.en.html` | 이용약관 (하단 메뉴) |
| `Privacy.ko.html` / `Privacy.en.html` | 개인정보 처리방침 (하단 메뉴) |
