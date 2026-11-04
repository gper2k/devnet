# BitPlus.Web.Site Views/Manager (관리자 화면)

| 파일 | 역할 |
| :--- | :--- |
| `Account.cshtml` | 계정 관리 콘솔. 탭 조각(`Account/*.cshtml`)을 fetch 로 꽂고, 조각에 필요한 JS(조직도 트리, 구성원 목록, 그룹/사용자 모달 저장)를 모두 이 파일에서 초기화 |
| `Account/Policy.cshtml` | 권한 정책 탭(역할 탭 다음): 구성 파일 + DB 병합 목록(출처 배지 구성/DB/덮어쓰기 — 적용 중인 쪽만 강조, DB 정책 활성 토글, 행 클릭 편집) + 정책 모달(정책명·설명·유효 기간 periodEdit·허용 대상 targetEdit, 구성 파일 정책이 있으면 읽기 전용 요약). 구성 파일 전용 행을 누르면 그 값을 초기값으로 한 같은 이름의 DB 덮어쓰기 등록. 로직은 `Account.cshtml` 의 `wirePolicyPage`, 스타일 `Policy.cshtml.scss` |
| `Account/Users.cshtml` | 그룹 조직도 트리 + 구성원 목록(하위 그룹 + 소속 사용자, 검색/표시 개수/페이저/표시·활성화 토글) + 그룹 모달(기본정보/속성/역할) + 사용자 모달(기본정보/속성/그룹/역할) 마크업 |
| `Content.cshtml` | 컨텐츠 관리 콘솔(/Manager/Content). 탭 "게시판"(`Content/Boards.cshtml`, `api/content/rules`)·"페이지 컨텐츠"(`Content/Pages.cshtml`, `api/content/list?type=PageView`, 작성·수정은 `/view/edit`) 조각을 꽂고 두 탭 목록을 공용 `wireList` 로 초기화. 스타일 `Content.cshtml.scss`(조각까지 `::deep`) |
| `Content/Board.cshtml` | 관리자 게시판 설정(/Manager/Content/Board/{규칙}, 신규 `?type=`) — 사용자 화면 `/content/manage` 와 같은 형식별 Manage 파샬뷰를 관리자 레이아웃(`div.pageBox.boardSettings`)으로 감싸고 저장 후 `/Manager/Content` 로 돌아감(`ViewData["ReturnUrl"]`). 관리자 전용 모양은 `Board.cshtml.scss`(파샬뷰 제목줄 숨김, 탭 본문만 스크롤, 저장 버튼 줄을 `.pageFooter` 모양으로 화면 아래 고정) |
| `External.cshtml` | 외부 서비스 콘솔(/Manager/External). 탭 SMTP(`External/Smtp.cshtml`, api/config/smtp), 외부 연동(`External/Integration.cshtml`, api/config/integration), Redis(`External/Redis.cshtml`, api/config/redis, 테스트·재시작 후 적용), AI API(`External/Ai.cshtml`, api/config/ai, 목록 + 등록/편집 모달), 접근 토큰(`External/Token.cshtml`, api/apitoken, 목록·삭제 + 발급 모달) 조각을 꽂고 탭별 초기화 함수를 이 파일에 둠 |
| `System.cshtml` | 시스템 설정(/Manager/System). 탭 사이트 정보(`System/Site.cshtml`)·다국어 관리(`System/Language.cshtml`)·설정 관리(`System/Config.cshtml`)·로그(`System/Log.cshtml`, api/log) 조각을 꽂고 탭별 초기화 함수를 이 파일에 둠. 로그 탭: 종류·날짜·시간 범위·수준·검색어로 보기, 행 클릭 시 원본 JSON 모달. "로그 제외 규칙" 버튼(또는 상세 모달의 "로그 제외 규칙 추가")으로 규칙 모달(줄마다 카테고리 앞부분 · 이 수준 이하 · 포함 글자, 기본값 되돌리기)을 열어 저장하면 바로 적용. 규칙 모달의 "현재 선택된 로그 제외"는 목록에 보이는 종류(검색어가 있으면 포함 글자로)를 Information 이하 제외 줄로 넣고 바로 저장 |
| `Service.cshtml` | 서비스 관리(/Manager/Service) 탭 페이지(탭 순서: 사이트 메뉴 · 팝업 · 이벤트 서비스). 탭 이벤트 서비스(`Service/List.cshtml`, api/service — 목록·상태·활성 토글, 행 클릭 시 설정 모달에 `Service/Forms/{서비스 이름}.cshtml` 양식 조각(/Manager/Service/Form/{이름})을 붙임, 사이트 메뉴(`Service/Menu.cshtml`, api/sitemap), 팝업(`Service/Popup.cshtml`, api/popup) 조각을 꽂고 탭별 초기화 함수(wireServicePage·wireMenuPage·wirePopupPage)를 이 파일에 둠. 새 서비스 설정 양식은 `Service/Forms/` 에 서비스 이름으로 추가(입력 name = 서비스 설정 값 키, 공용 AI 접속 선택 조각 `Service/Forms/_AiConnection.cshtml`) |
| `Content/FileRule.cshtml` | 컨텐츠 관리 "파일 규칙" 탭(신고 탭 앞, 목록·등록/수정/삭제 모달·권한 규칙, 로직은 `Content.cshtml` 의 `wireFileRulePage`) + `FileRule.cshtml.scss` |
| `Service/Menu.cshtml` | 서비스 관리 "사이트 메뉴" 탭(팝업 탭 앞): 왼쪽 메뉴 트리(확장 메뉴: 하위 메뉴 추가/편집/삭제) + 오른쪽 선택 메뉴(없으면 최상위)의 하위 목록(드래그 순서 변경, 사용 토글, 행 클릭 편집) + 메뉴 모달(기본 정보/접근 권한). 초기화는 `Service.cshtml` 의 `wireMenuPage` |
| `Account/Config.cshtml` | 계정 구성 카드 화면. 가입 기본 그룹/역할은 기본 정보 카드의 "기본 그룹 설정"·"기본 역할 설정" 버튼으로 여는 모달(`joinGroupModal`·`joinRoleModal`, 칩 목록)에서 고치고 하단 저장으로 함께 저장. 모달 크기는 `Config.cshtml.scss` |
| `Account/Users.cshtml.scss` | 위 화면 전용 스타일 (그룹/사용자 모달 고정 크기 34em × 탭 27em) |

## 동작 요약
- 조직도: `api/membership/group/tree` 실데이터, 맨 위 가상 항목 "전체 사용자"(키 -1, 확장메뉴 없음). 확장메뉴: 그룹 추가 / 사용자 추가 / 편집 / 이동 / 삭제. 저장·이동·삭제는 `orgTree.upsert/remove` 로 부분 반영
- 그룹 모달: 상위 그룹 "찾기"(groupSelect, 자기/하위 선택 불가), 그룹명 수정 불가, 속성(keyValueBox), 역할(chipBox + roleSelect)
- 사용자 모달: 사용자 ID 수정 불가(최소 길이 `data-min` = `AccountRule.UidMinSize`), 이메일/전화번호, 속성, 그룹(chipBox `data-sync`, 관리 버튼), 역할(chipBox 추가 방식), 표시/Enabler
- 저장 요청은 `common.makeFormData` 로 개별 폼 필드 전송, 모달은 열 때마다 첫 탭으로 초기화
