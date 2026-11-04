# BitPlus.Web.Site Models

화면(뷰)에 넘기는 뷰 모델. API 요청 바인딩용 DTO 는 두지 않습니다 (API 는 개별 폼 필드로 받음). 네임스페이스 `BitPlus.Web.Site.Models` 는 `Views/_ViewImports.cshtml` 에서 가져옵니다.

| 파일 | 설명 |
| :--- | :--- |
| `ContentPageModel.cs` | 컨텐츠 페이지(`/content/{ruleNo}/{view?}`) 뷰 모델 — 규칙 번호, 제목, 형식, 열 파샬뷰 이름과 경로(`ViewPath`). 구조는 `Views/Content/README.md`. |
| `ContentPermission.cs` | 컨텐츠 규칙에 대한 현재 사용자 권한(관리자/읽기/쓰기) 판정. 규칙은 `Views/Content/README.md`. |
| `ContentListItem.cs` | 컨텐츠 목록의 글 한 줄(번호, 제목, 글쓴이, 공지, 조회, 등록일). |
| `ContentPortalModel.cs`·`ContentPortalItem.cs` | 컨텐츠 포탈(`/content`, 메뉴별 `/content/menu/{메뉴 키}` 는 `MenuKey`) 뷰 모델 — 컨텐츠 규칙 목록(규칙 번호, 제목, 형식, 글 수). 구조는 `Views/Content/README.md`. |
| `ContentViewModel.cs` | 컨텐츠 보기 페이지(`/view/{contentNo}`) 뷰 모델 — 규칙/글 번호, 제목, 본문(HTML 그대로), 등록 시각. 구조는 `Views/View/README.md`. |
| `ContentEditModel.cs` | 컨텐츠 작성/수정 페이지(`/view/edit/{contentNo?}`) 뷰 모델 — 글 번호(주소에 없으면 신규, 내부 값 -1), 에디터 소유자 키, 제목, 본문. |
| `SiteInfo.cs` | 사이트 기본 정보(설정 `SiteInfo`, AppConfig 범주) — 사이트 제목·검색 키워드·설명·파비콘·공유 이미지(문서 머리), 로고·사이트명·소개 문구·카피라이트(상단·하단), 회사명·전화·메일·주소(하단 연락처), 주 에디터(`Editor`, 루트 레이아웃이 `new Site({ editor })` 로 넘겨 모든 글쓰기 화면에 적용, 비우면 클라이언트 기본). `SiteInfo.Of(config)` 가 저장값 또는 기본값(설치 전처럼 읽기 실패해도 기본값). 관리자 "시스템 설정 > 사이트 정보" 에서 저장. |
| `SiteIntegration.cs` | 외부 서비스 연동 정보(설정 `SiteIntegration`, AppConfig 범주) — 브라우저에 내려가는 공개용 키만(카카오 JavaScript 키 `KakaoJsKey`, GA4 측정 ID `GaMeasurementId`). `SiteIntegration.Of(config)`. 관리자 "시스템 설정 > 외부 연동" 에서 저장. |
| `UserPageModel.cs` | 회원 페이지(`/user/...`) 뷰 모델 — 탭, 본인 여부, 이름·이미지·가입일, 내 페이지면 ID·메일(인증)·전화·소개·마지막 접속·그룹/역할(활성)·안 읽은 메시지 수·메시지 받기. |
