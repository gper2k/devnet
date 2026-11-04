# BitPlus.Web.Site/Views/Search (통합 검색 화면)

| 파일 | 역할 |
| :--- | :--- |
| `Index.cshtml` | 통합 검색(`/search?q=&type=&page=`) — 검색어 입력, 구분 탭(전체 + 공급자별 건수), 결과(제목·묶음 이름·날짜·본문 일부, 검색어 `mark`). 전체는 구분마다 5건과 "더 보기", 구분을 고르면 20건씩 페이지 이동 |
| `Index.cshtml.scss` | 위 화면 전용 스타일 (compilerconfig 등록) |
