# Views/Shared

공용 레이아웃 뷰 폴더.

- `_Layout.cshtml` — 최상위 레이아웃 (site.js/site.css 로드).
- `_PageLayout.cshtml` — 일반 공개 페이지 레이아웃.
  - 상단 메뉴: DB "User" 메뉴 자손(`MenuReader.ReadTreeAsync`)을 2단까지. 하위가 있는 1단은 이름 끝 화살표(`fiChevronDown`), 하위는 안쪽 `ul` — 데스크톱은 마우스 오버/포커스 때 1단 아래 가운데에 좌우가 둥근 가로 바(페이드 + 살짝 내려옴), 사이드 패널(1024 이하)은 하위 메뉴를 늘 펼쳐 보임(화살표 숨김, 왼쪽 세로줄 + 들여 쓴 항목) — 메뉴 목록(`nav > ul`)이 패널 높이를 넘으면 목록만 스크롤. 하위가 지금 페이지면 1단도 `active`. 이동/새 창은 메뉴 `ul` 클릭 위임(`data-href`/`data-open`).
  - 사이드 패널 하단(780 이하): 통합 검색 바로가기(`nav > a`, 검색창 모양 pill → /search) 위, 인증 상자 아래. 인증 상자는 로그인 전 로그인·회원가입 두 줄(이미지·열쇠 아이콘도 로그인), 로그인 후 이름·그룹/역할·알림/메시지 수 링크.
- `_AdminLayout.cshtml` — 관리자 콘솔 레이아웃.
- `_AccountLayout.cshtml` — 로그인/회원가입 등 계정 화면 레이아웃.
  - 왼쪽: 회사명(Bitplus) / 페이지 제목(`Title`) + 방문 환영 문구(WelcomeVisit) / 언어 선택 드롭다운(공용 `.dropDown`, `ul.up` 으로 위로 펼침, 선택 시 `common.setCulture` → Culture 쿠키 저장 후 새로고침).
  - 오른쪽 상단 메뉴: 로그인 / 아이디·비밀번호 찾기 / 회원가입 중 현재 페이지(`PageKey`)를 제외하고 표시.
  - 스타일: `_AccountLayout.cshtml.scss` → `_AccountLayout.cshtml.css`, `wwwroot/css/accountLayout.css` (compilerconfig.json).
- 주의: Razor 런타임 컴파일 환경에서는 컬렉션 식(`["a","b"]`)이 컴파일되지 않으므로 뷰에서는 `new[] { ... }` 를 사용한다.
