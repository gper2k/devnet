# Views/User

회원 페이지 뷰 (`Controllers/UserController.cs`, 모델 `Models/UserPageModel.cs`). 모두 `_PageLayout` 안에서 열리고 로그인 사용자만 볼 수 있습니다. 데이터 처리는 `api/user`(`Controllers/API/UserController*.cs`)와 `api/account/password` 입니다.

| 파일 | 주소 | 설명 |
| :--- | :--- | :--- |
| `_UserHead.cshtml` | (부분 뷰) | 머리: 이미지(없으면 사람 아이콘)·이름(내 페이지면 ID)·가입일. 내 페이지면 탭(내 정보·메시지(안 읽은 수)·작성글 보기·환경설정), 다른 사용자면 메시지 보내기 버튼. 정보 탭에서는 이미지를 누르면 등록/교체, 등록된 이미지가 있으면 초기화 버튼. |
| `Index.cshtml` | `/user` | 내 정보: 기본 정보 카드(`li.row` 가로형 — ID·표시명·메일(인증 여부)·전화·마지막 접속, 비밀번호 변경·정보 편집 버튼과 모달; 정보 편집은 다국어 표시명(관리자 사용자 편집과 같은 `input[data-lang]`)·메일·전화, ID 고정, `PUT api/account/profile`), 그룹·역할 카드(활성 강조), 외부 로그인 연결 카드(행마다 브랜드 로고(`brandIcon` SVG, 없으면 Fluent 아이콘)·이름 | 상태 뱃지(연결됨 = 초록, 미연결 = 누르면 연결하는 주색 "계정 연결" 뱃지) | 연결됨이면 오른쪽 끝 작은 "연결 해제" 글자, 모두 세로 가운데 정렬. 스크립트가 만드는 요소에는 공용 `mk()` 로 범위 CSS 속성(b-xxx)을 붙임). 이미지는 에디터 파일 API(소유자 `user.photo`, 키 = 내 식별키)로 올린 뒤 `PUT api/user/photo`, 초기화는 `DELETE api/user/photo`. |
| `Notice.cshtml` | `/user/notice` | 알림: 시스템이 보낸 일방 알림 목록(최신순 20개씩, api/user/notice). 확인 안 한 알림 강조, 줄마다 확인·삭제, 위에 모두 확인·확인한 알림 지우기. 문구에 주소가 있으면 누를 때 확인 처리 후 이동. |
| `Message.cshtml` | `/user/message` | 메시지: 왼쪽 대화 상대 목록(이름·메시지 수·안 읽은 수, 최근순), 오른쪽 메신저형 대화(내 메시지 오른쪽, 메시지마다 작은 삭제 버튼, 아래 입력창). 대화를 열면 받은 메시지 읽음 처리. 처음에는 가장 최근 상대. |
| `Posts.cshtml` | `/user/posts`, `/user/{식별키}` | 작성글 보기: 게시물/댓글 탭(개수)과 목록(`api/user/{식별키}/activity`, 20개씩). 다른 사용자 주소는 게시판 작성자 메뉴의 "작성글 보기", 내 식별키면 `/user/posts` 로 이동. |
| `Setting.cshtml` | `/user/setting` | 환경설정: 언어(`Culture` 쿠키, `common.setCulture`), 테마(`Theme` 쿠키 — `_Layout` 이 `data-theme` 로 적용), 메시지 받기(`PUT api/user/setting`, 끄면 다른 회원이 보낼 수 없음). |
