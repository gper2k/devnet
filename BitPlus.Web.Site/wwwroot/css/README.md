# BitPlus.Web.Site SCSS 및 CSS 아키텍처 가이드

본 디렉터리는 사이트 전역 스타일시트를 구성하는 SCSS 모듈 및 컴파일된 CSS 결과물들을 관리합니다.

---

## 1. 파일 구성 및 역할 분담

모든 스타일은 목적에 따라 5개의 핵심 SCSS 파셜 파일로 분할 관리되며, `Site.scss`를 통해 단일 `Site.css` (및 `site.min.css`) 파일로 통합 빌드됩니다.

| SCSS 파일 | 주요 역할 및 책임 | 대상 요소 및 컴포넌트 |
| :--- | :--- | :--- |
| **`_Document.scss`** | • 9대 테마색 및 밝기(0~9) CSS 변수 정의<br>• 다크 테마(`[data-theme="dark"]`) 및 레이아웃 오버라이드<br>• BaseUI 표준 HTML, body, #app 및 a 태그 스타일 | • `:root`, `[data-theme="dark"]`, `.adminLayout` 등<br>• `html`, `body`, `#app`, `div#app`<br>• `a` (일반 텍스트 색상 및 밑줄 제거)<br>• 스크롤바 표준화 |
| **`_Controls.scss`** | • 사용자 상호작용 및 데이터 입력 컨트롤 요소 | • `button`, `.btn` (크기, 테마 색상, 아웃라인)<br>• `input`, `textarea`, `select`<br>• `input[type="checkbox"]`, `input[type="radio"]`<br>• `.checkLabel`, `.radioLabel`<br>• 토글 스위치(`.switch`) |
| **`_Area.scss`** | • 화면 레이아웃 구획 및 컨테이너 영역 | • `.container`<br>• `.siteHeader`, `.siteFooter`, `.siteSidebar`, `.siteMain`<br>• 카드/패널(`.card`)<br>• 좌우 분할 영역(`.splitArea`)<br>• 반응형 그리드 영역(`.gridArea`) |
| **`_Complex.scss`** | • 영역(Area)과 컨트롤(Control) 중간의 복합 UI 블록 | • 인풋 그룹(`.inputGroup`)<br>• 탭 박스(`.tabBox`)<br>• 드롭다운 메뉴(`.dropDown`)<br>• 아코디언(`.accordion`)<br>• 트리 목록(`.treeBox`)<br>• 상태 뱃지(`.badge`)<br>• 페이지네이션(`.paging`)<br>• 모달 대화상자(`.modalBox`)<br>• 툴바/액션바(`.toolBar`)<br>• 키-값 속성 상자(`.keyValueBox`: 키 + 값 입력/시스템 속성 span + 삭제, 하단 추가 줄)<br>• 칩 목록 상자(`.chipBox`: 기본 지정 라디오 + 삭제, 하단 추가/관리 버튼)<br>• 선택 목록(`ul.selectList`)<br>• 중첩 모달: 첫 모달(`dialog[data-first]`)만 배경 어둡게, 최상단 이전 모달(`dialog[data-under]`)은 블러 |
| **`_Util.scss`** | • 기타 유틸리티 및 헬퍼 클래스 | • Flexbox (`.flexRow`, `.flexCol`, `.center`, `.between`)<br>• 간격 (`.p0`, `.pSm`, `.pMd`, `.m0`, `.mSm`, `.mMd`)<br>• 텍스트 정렬, 크기, 굵기 및 색상 유틸리티<br>• 안내 박스(`.information`), 로딩 스피너(`.spinner`)<br>• 유효성 검사 토스트(`div.validMsg[popover]`, 우하단 표시 후 위로 올라가며 자동 제거) |
| **`Site.scss`** | • 5개 모듈 일괄 통합 엔트리포인트 | `@use "Document";`<br>`@use "Controls";`<br>`@use "Area";`<br>`@use "Complex";`<br>`@use "Util";` |

---

## 2. 스타일 작성 시 엄격 준수 규칙 (Core Principles)

### 1) 중간바(`-`) 금지 및 카멜케이스(camelCase) 명명
- 클래스 및 식별자 작명 시 케밥케이스(`-`)를 절대 사용하지 않고 `camelCase`로 작성합니다. (예: `inputGroup`, `tabBox`, `dropDown`, `siteHeader`).
- 사용 편의성을 위해 불필요하게 긴 이름을 지양하고 1자라도 덜 쓸 수 있도록 최대한 간결하고 직관적으로 명명합니다 (예: `.btn`, `.card`, `.p0`, `.pSm`, `.txLeft` 등).

### 2) 클래스 남발 금지 (단일 루트 클래스 원칙)
- **루트가 되는 요소에만 단 하나의 클래스를 지정**합니다.
- 하위 자식/자손 노드는 클래스를 추가하지 않고, **HTML 태그 선택자(`& > header`, `& > section`, `& > nav > ul > li > a` 등) 및 자식/인접 선택자를 활용하여 계층적으로 스타일을 전개**합니다.
- 동일 부모 내에서 동일 태그가 서로 다른 독립적인 역할을 수행해야 하는 극히 예외적인 경우에만 보조 클래스를 허용합니다.

### 3) 9대 테마색 체계 및 밝기 구분 (0~9)
- **주 색상(Primary Brand Color)**: **`main`**이 시스템의 핵심 주 색상으로 기본 동작합니다.
- **9대 테마색**: `main`, `sub`, `point`, `deco`, `primary`, `info`, `success`, `warning`, `danger`
- **변수 규격**:
  - 기본: `--{color}`
  - 다크/라이트: `--{color}Dark`, `--{color}Light`
  - 밝기 단계: `--{color}0` ~ `--{color}9` (0이 가장 밝은 틴트, 9가 가장 어두운 셰이드)
- **알파 음영 계층 (`aColor`)**: 기존 `gsColor`에서 `aColor`로 명칭을 통일(`--aLow`, `--aLight`, `--aColor`, `--aDark`, `--aHigh`, `--aDeep`)하여 일관된 검은색 반투명 입체감과 다크 테마 음영을 제공합니다.
- **원시 색상 코드(Raw Color) 작성 금지**: 셀렉터 내부에서 `#fff`, `rgb(...)` 등 raw 색상 작성을 전면 금지하며, 승인된 변수(`var(--...)`)만 사용합니다.

### 4) 버튼 및 입력항 인터랙션/스타일링 표준
- **컨트롤 공통 기본 스타일 일원화 (button, .btn, input, select)**:
  - 버튼과 입력항이 나란히 배치될 때 완벽한 정렬을 위해 `height(2.6em)`, `padding(0.5em 0.8em)`, `border-radius(var(--roundBtn))`, `font-size(1em)`, `box-sizing`, `vertical-align`, 기본 색상/배경, 공통 `disabled` 상태 및 `@each $size in $sizes` 스케일링을 단일 공통 선택자(`button, .btn, input, select`)로 일괄 선언하여 완전한 DRY 구조를 유지합니다.
  - 라운드 크기나 패딩, 높이 등 공통 규격 변경 시 한 곳의 수정만으로 버튼과 폼 입력항에 동시에 일관되게 반영됩니다.
- **높이 및 Pill 라운딩 일치**: 버튼(`button, .btn`)과 입력필드(`input, select`)는 100% 동일한 **`2.6em`** 높이 및 Pill 라운딩(`--roundBtn: 2em`)으로 대시보드 룩앤필과 완벽히 정렬됩니다.
- **`aColor` 블랙 반투명 외곽선 원칙**:
  - 버튼 외곽선에 `border: 1px solid var(--aColor);`를 기본 적용하여 배경과의 경계를 명확히 구분합니다.
  - 테마 색상이 주입되었을 때 블랙 반투명이 배경색과 자연스럽게 블렌딩되어 **해당 색상의 짙은 음영 테두리**를 자동으로 형성하므로, 개별 색상 클래스마다 `border-color`를 재정의할 필요 없이 완전한 DRY 구조를 유지합니다.
- **공통성 준수 (특정 색상 예외 처리 금지)**:
  - `_Document.scss`는 공용 베이스 스타일이므로 `point` 등 특정 색상에 대한 별도 분기나 하드코딩을 일체 배제합니다.
  - 모든 9대 테마색은 `:root`의 `--{color}Text` 변수를 통해 가독성 텍스트 색상을 선언적으로 관리하며, `@each` 루프에서 100% 대칭적이고 동일한 메커니즘으로 처리합니다.
  - 포커스 링 또한 특정 색상 대신 공통 시맨틱 변수(`--lineFocusColor`, `--aColor`)를 사용합니다.
- **이동 효과(translate) 배제 및 단정한 클릭 피드백**: 불필요한 위치 이동(`translateY`)을 전면 제거하고, 호버 시 이동 없는 깔끔한 검은색 반투명 그림자(`box-shadow: 0 4px 12px var(--aDark)`), 클릭 시 미세한 스케일 다운(`scale(0.98)`)과 안쪽 그림자(`box-shadow: inset 0.1em 0.12em 0.25em var(--aHigh)`)로 물리적 깊이감을 확실히 전달합니다.
- **`:not` 선택자 배제 및 선택자 우선순위 방어 (Specificity Protection)**:
  - `:not(:disabled)`나 `:not(:read-only)` 남발을 전면 배제하고, CSS 기본 캐스케이드 및 `:disabled { pointer-events: none; }`을 통해 인터랙션을 단정하게 분기합니다.
  - 기본 스타일은 `input, select`로 단순하고 직관적으로 선언하고, 체크박스/라디오/레인지/파일 등 비텍스트 입력항은 별도의 리셋 무력화 규칙(`input[type="checkbox"] { width: auto; ... }`)으로 처리합니다.
- **파일 업로드 버튼 투명 오버레이 표준**:
  - 파일 업로드 요소는 브라우저 기본의 부자연스러운 UI 대신, 버튼(`button, .btn` 또는 `.fileBtn`) 전체를 완전히 덮는 투명 오버레이(`opacity: 0; position: absolute; inset: 0; width: 100%; height: 100%; cursor: pointer;`)로 구현합니다.
  - 사용자는 일반 버튼의 룩앤필과 호버/액티브/테마 색상 효과만을 경험하며, 버튼 어디를 클릭하든 OS 파일 선택창이 자연스럽게 호출됩니다.
  - `<button type="button">`, `<label class="btn">`, `.fileBtn` 등 다양한 마크업 구조에서 동일한 효과를 완벽 지원합니다.

### 5) 테마 및 레이아웃 오버라이드
- **다크 테마(`[data-theme="dark"]`)**: 테마 변수 및 표면색(`--bgColor`, `--txColor`, `--lineColor` 등)의 오버라이드로 동작.
- **레이아웃별 독자 색상**: `.pageLayout`, `.adminLayout`, `.accountLayout` 또는 `[data-layout="..."]`에서 변수를 재정의하여 레이아웃별로 자유롭게 색상을 구성.

### 6) HTML / body / #app / a 태그 기본 규칙 (BaseUI 호환)
- `html, body, #app, div#app`: `display: block; margin: 0; padding: 0; float: left; width: 100vw; height: 100vh;`
- `a` 태그: 기본 밑줄 제거 및 일반 본문 텍스트와 동일한 글자색(`color: var(--txColor); text-decoration: none;`) 유지.

### 7) 단위 표준화
- 타이포그래피, 패딩, 마진, 간격 등 크기 단위는 기본적으로 **`em`** 단위를 사용합니다.
- 컴포넌트 조상에 `font-size: 1rem`을 설정한 후, 하위 자손 요소들의 크기 및 간격을 `em`으로 비례 조정합니다. 고정 물리 크기가 필수적인 특수 상황을 제외하고 `px`의 직접 사용을 지양합니다.

### 8) `!important` 사용 금지 및 최소화 원칙
- **`!important` 사용 엄격 배제**: `!important`를 사용하면 향후 컴포넌트 레벨 오버라이드, 레이아웃 변경, 테마 커스터마이징이 극히 어려워지거나 불가능해지므로 스타일시트 전반에서 `!important` 사용을 엄격히 배제합니다.
- CSS 기본 캐스케이드 순서(Source Order) 및 명시도(Specificity), 구조적 중첩 규칙에 의존하여 자연스럽고 예측 가능한 스타일 오버라이드를 보장합니다.

---

## 3. 컴파일 및 빌드 도구
- **Visual Studio IDE**: `compilerconfig.json`을 통해 `Site.scss` 저장 시 `Site.css` 및 `site.min.css` 자동 컴파일 지원.
- **CLI / Dart-Sass**:
  ```bash
  # 표준 CSS 컴파일
  npx sass wwwroot/css/Site.scss wwwroot/css/Site.css --no-source-map

  # 압축 배포용 Minified CSS 컴파일
  npx sass wwwroot/css/Site.scss wwwroot/css/site.min.css --style compressed --no-source-map
  ```
