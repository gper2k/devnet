# Views/User

회원 페이지 뷰 (`Controllers/UserController.cs`, 모델 `Models/UserPageModel.cs`). 모두 `_PageLayout` 안에서 열리고 로그인 사용자만 볼 수 있습니다. 데이터 처리는 `api/user`(`Controllers/API/UserController*.cs`)와 `api/account/password` 입니다.

| 파일 | 주소 | 설명 |
| :--- | :--- | :--- |
| `_ViewStart.cshtml`, `_UserLayout.cshtml` | (레이아웃) | 회원 레이아웃: 상위 `_ViewStart` 가 정한 사이트 레이아웃 안에 한 겹 더 씌움. 내 페이지면 왼쪽 어두운 사이드 메뉴 카드(`aside#pageMenu`, `data-theme="dark"` 로 카드 안 테마 변수만 어두운 값) — 이미지(누르면 등록/교체, 등록된 이미지가 있으면 초기화 버튼)·이름·ID·가입일, 탭(내 정보·알림·메시지(안 읽은 수)·작성글 보기·환경설정), 활성 그룹/역할 — 와 오른쪽 본문. 1024px 이하에서는 사이트 레이아웃의 우측 서랍(우상단 ⋮)으로 열림. 다른 사용자 페이지는 본문만. |
| `_UserHead.cshtml` | (부분 뷰) | 다른 사용자 페이지 머리: 이미지(없으면 사람 아이콘)·이름·가입일, 메시지 보내기 버튼. |
| `Index.cshtml` | `/user` | 내 정보: 기본 정보 카드(항목은 이름 위·값 아래 세로형 — ID·표시명·메일(인증 여부)·전화·마지막 접속, 아래로 정보 편집·비밀번호 변경·회원 탈퇴 버튼을 세로로 쌓음, 모달; 정보 편집은 다국어 표시명(관리자 사용자 편집과 같은 `input[data-lang]`)·메일·전화, ID 고정, `PUT api/account/profile`), 그룹·역할 카드(활성 강조), 외부 로그인 연결 카드(행마다 브랜드 로고(`brandIcon` SVG, 없으면 Fluent 아이콘)·이름 | 상태 뱃지(연결됨 = 초록, 미연결 = 누르면 연결하는 주색 "계정 연결" 뱃지) | 연결됨이면 오른쪽 끝 작은 "연결 해제" 글자, 모두 세로 가운데 정렬. 스크립트가 만드는 요소에는 공용 `mk()` 로 범위 CSS 속성(b-xxx)을 붙임). 우측은 활동 요약 카운터·최근 작성 게시물/댓글, 그 아래 외부 로그인 연결 카드. 사이드 메뉴 옆이 좁은 폭(1025~1200px)과 780px 이하는 1단. |
| `Notice.cshtml` | `/user/notice` | 알림: 시스템이 보낸 일방 알림 목록(최신순 20개씩, api/user/notice). 확인 안 한 알림 강조, 줄마다 확인·삭제, 위에 모두 확인·확인한 알림 지우기. 문구에 주소가 있으면 누를 때 확인 처리 후 이동. |
| `Message.cshtml` | `/user/message` | 메시지: 왼쪽 대화 상대 목록(이름·메시지 수·안 읽은 수, 최근순), 오른쪽 메신저형 대화(내 메시지 오른쪽, 메시지마다 작은 삭제 버튼, 아래 입력창). 대화를 열면 받은 메시지 읽음 처리. 처음에는 가장 최근 상대. |
| `Posts.cshtml` | `/user/posts`, `/user/{식별키}` | 작성글 보기: 게시물/댓글 탭(개수)과 목록(`api/user/{식별키}/activity`, 20개씩). 다른 사용자 주소는 게시판 작성자 메뉴의 "작성글 보기", 내 식별키면 `/user/posts` 로 이동. |
| `Setting.cshtml` | `/user/setting` | 환경설정: 언어(`Culture` 쿠키, `common.setCulture`), 테마(`Theme` 쿠키 — `_Layout` 이 `data-theme` 로 적용), 메시지 받기(`PUT api/user/setting`, 끄면 다른 회원이 보낼 수 없음). |
