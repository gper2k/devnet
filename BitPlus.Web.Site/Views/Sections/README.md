# BitPlus.Web.Site/Views/Sections (영역·위젯 페이지 정의)

영역(Section) 하나 = 폴더 하나 (폴더 이름 = 영역 키). 사이트 시작 때 `section.json` 을 읽어 `SectionTypes` 테이블에 등록합니다 (`Widgets/WidgetCatalog`). 화면과 편집 모드 썸네일이 같은 정의로 그려집니다.

`section.json`: key, version, order(목록 순서), name·description(문화권별), 그리고
- 고정형: `slots`(슬롯 키) + `layouts`(`1024`·`1023`·`779`·`519` 각각 CSS grid 영역 줄 배열, 예 `["a a b b", "a a c c"]`, 빈칸 ".")
- 가로형: `repeat`(`{min, max, default}`) — 칸 수를 배치 때 고르고, 줄은 자동(780 미만 2칸, 520 미만 1칸씩)

| 영역 | 칸 | 1024 이상 |
| :--- | :--- | :--- |
| `Full` | a 4×1 | 한 칸 전체 폭 |
| `Horizontal` | 2~6칸 (각 4/n×1) | 가로로 늘어놓음 |
| `Grid2x2` | a 2×2, b 2×1, c 2×1 | 왼쪽 넓은 칸 + 오른쪽 작은 칸 둘 |
| `Split31` | a 3×1, b 1×1 | 넓은 본문 + 좁은 사이드 |
| `Split21` | a 2/3폭×1, b 1/3폭×1 (3단 격자) | 배너 + 기사 목록 |

`_Pages/{페이지}.json` = 위젯 페이지 정의: key, name, categories(쓸 수 있는 위젯 카테고리, `*` = 모두), layout(기본 배치 — 설정 `PageLayout.{페이지}` 가 없을 때; 게시판 위젯 ruleNos 는 시드 게시판 별칭 `"@별칭"` 으로 적고 설치 때 번호로 바뀜). 지금 `home`(모두), `portal`(모두), `side`(모두 + `side` 전용 위젯 — 컨텐츠 사이드 패널, 좁은 칸이라 `Full` 영역 권장, 기본 배치 맨 위 `ContentMap` 트리 위젯).
페이지 키에 `@{메뉴 키}` 를 붙인 변형은 배치를 따로 저장하고, 저장한 적 없으면 앞 페이지 배치를 씁니다: `side@{메뉴 키}`(사이드 연결 메뉴별), `portal@{메뉴 키}`(메뉴별 포탈 `/content/menu/{메뉴 키}`).

`portal` 기본 배치: Split21(Banner·LatestPosts 기사 목록) → Grid2x2(LatestPosts·Notice·LatestPosts) → Gallery.

`home` 기본 배치: Hero → FeatureCards → Showcase → FeatureTabs → Stats → Grid2x2(LatestPosts·Notice·CallToAction) → Gallery.
