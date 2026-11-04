# BitPlus.Web.Site/Search (통합 검색 공급자)

`BitPlus.Web/Search` 의 `ISearchProvider` 구현입니다. 시작할 때 자동으로 찾아 등록되며, 통합 검색 화면(`/search`, `SearchController`)에 구분으로 나옵니다.

| 파일 | 역할 |
| :--- | :--- |
| `ContentSearchProvider.cs` | 컨텐츠(Contents 테이블) 공급자 공통 — 표시·게시·게시 시작이 지난 글 중 제목·본문에 검색어가 든 글(최신순) 후보, 메모리 페이지 나누기. 한계: LIKE 검색 + 최신 500건 안에서 권한을 거름 |
| `PageSearchProvider.cs` | "페이지"(type=page, 순서 1) — 보기 전용 규칙의 페이지(/view/{번호}), 게시 기간 안, 읽기 대상이 있으면 대상·관리자만 |
| `BoardSearchProvider.cs` | "게시판"(type=board, 순서 2) — 사용 중인 게시판 글, 글 보기와 같은 판정(`PostAccessRule.Check`)을 통과한 글만, 묶음 이름 = 게시판 이름, 주소 = 글 보기(FAQ 는 목록, 포럼 댓글은 토픽) |
