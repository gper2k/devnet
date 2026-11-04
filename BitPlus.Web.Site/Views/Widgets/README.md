# BitPlus.Web.Site/Views/Widgets (위젯)

위젯 하나 = 폴더 하나 (폴더 이름 = 위젯 키, 영문·숫자). 사이트 시작 때 `widget.json` 을 읽어 `WidgetTypes` 테이블에 등록합니다 (`Widgets/WidgetCatalog`).

| 파일 (폴더마다) | 역할 |
| :--- | :--- |
| `widget.json` | key, version(올리면 정보 갱신), name·description(문화권별), categories(`general`·`board` …, 페이지가 쓸 수 있는 것만 고름 — `side` 는 사이드 패널 전용으로 페이지 정의에 `side` 를 직접 적은 페이지에만, `*` 로는 허용 안 됨), size.min·max(`[가로 칸, 세로 행]`, 가로 4칸 격자), settings(`[{key, type, default, min?, max?}]`), single(true 면 한 배치에 하나만, 기본 false), frame(false 면 공통 카드 틀·제목 없이 출력 뷰만 그림, 기본 true), cacheSeconds(예약) |
| `View.cshtml` | 출력 뷰 — 모델 `SlotView` (Settings, Data = 로더 결과). 카드 틀(제목)은 `_PageSections` 가 씌움 |
| `Edit.cshtml` | 설정 양식 조각 (Layout = null) — 입력 name = 설정 키, 값 채우기·모으기는 편집 모드 스크립트가 함 |
| `View.cshtml.scss` | 출력 스타일 (선택, compilerconfig 등록) |
| `_BoardSelect.cshtml` | 설정 양식 공용: 게시판 여러 개 고르기(name=ruleNos, 모델 = 고를 형식) |
| `_Design.scss` | 디자인 위젯 scss 공용 (`@use "../Design" as design;`): `underline(색)` = 제목 아래 짧은 막대, `label` = 제목 위 머리말 |
| `_DesignFields.cshtml` | 디자인 위젯 설정 양식 공용 (모델 = 묶음 이름 배열: heading = label·title·text, image, button = buttonText·buttonUrl, reverse, band) |

| 위젯 | 카테고리 | 크기 | 설정 |
| :--- | :--- | :--- | :--- |
| `ContentMap` | side (한 패널에 하나, 틀 없음) | 1×1 ~ 4×4 | 없음 — 사이드 패널 머리줄(제목·신규 게시판 +·게시판 설정 톱니, + · 톱니는 컨텐츠 페이지 영역에서만) + 메뉴 트리. 루트는 연결 메뉴(없으면 컨텐츠맵) |
| `Html` | general | 1×1 ~ 4×4 | html (저장할 때 정제) |
| `Banner` | general | 2×1 ~ 4×2 | items (줄마다 이미지 \| 링크 \| 문구), interval (초), height (px, 0 = 2:1) |
| `VerticalBanner` | general | 1×1 ~ 4×4 | Banner 와 같은 설정·출력(partial), 비율만 세로 3:4 |
| `BuyMeACoffee` | side, general (틀 없음) | 1×1 ~ 4×4 | url(후원 링크 주소), image(배너 이미지 경로, 기본 /img/banners/side_coffee.svg), target(새 창 여부) — 세로 3:4 개발자 커피 후원 배너 링크 |
| `LatestPosts` | board | 1×1 ~ 4×4 | ruleNos, count, summary (설문은 요약 안 함), image (대표 이미지, 있을 때만 왼쪽에) |
| `Notice` | board | 1×1 ~ 4×2 | ruleNos, count (공지글만), image (작은 대표 이미지) |
| `Gallery` | board | 2×1 ~ 4×3 | ruleNos (갤러리만), count, columns |
| `Hero` | design | 4×1 ~ 4×2 | label, title(형광펜), text(줄), image(노트북 화면), buttonText, buttonUrl, reverse — main1 바탕, 정면 CSS 노트북 + 노트북 쪽 절반에서 틀 밖으로 나가는 큰 톤 원, 글자 쪽 아래 겹치는 톤 원 |
| `FeatureCards` | design | 2×1 ~ 4×2 | label, title(밑줄 막대), text, items (줄마다 아이콘 \| 제목 \| 설명), columns — 카드 = 기운 아이콘 상자·큰 번호(01…), 올리면 main 배경 |
| `Showcase` | design | 4×1 ~ 4×2 | label, title, text, image, buttonText, buttonUrl, reverse, band(main 띠, 아래쪽 사선) — 배경에 이미지 쪽에서 글자까지 덮으며 틀 밖으로 나가는 큰 톤 원 두 개 |
| `FeatureTabs` | design | 4×1 ~ 4×2 | label, title(밑줄 막대), text, items (줄마다 제목 \| 설명 \| 이미지) — 누르거나 올리면 오른쪽 이미지(16:9) 바뀜 |
| `Stats` | design | 4×1 ~ 4×2 | label, title, text, image, bars (줄마다 이름 \| 값 0~100), band(위쪽 사선) — 배경에 이미지 쪽 밝은 원·반대쪽 짙은 원·글자 위쪽 옅은 원 |
| `CallToAction` | design | 2×1 ~ 4×2 | label, title, text, buttonText, buttonUrl — main1 바탕 가운데 상자 |

Fluent 아이콘(`.fi`)은 `background-color: currentColor` + mask 로 그리므로 아이콘 요소에 배경색을 주면 색이 흐려집니다. 원·상자 배경은 `<span><i class="fi …"></i></span>` 처럼 감싼 span 에 줍니다.

큰 톤 원은 위젯 뿌리의 ::before/::after(z-index -1, isolation)로 그려 글자 아래에 깔리고, 틀 밖으로 나간 부분은 잘립니다.
