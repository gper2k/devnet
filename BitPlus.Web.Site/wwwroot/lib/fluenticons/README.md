# ✨ Fluent Icons (BitPlus Edition)

> **Microsoft Fluent System Icons 기반의 초경량화(81.7 KB) & 고성능 CSS Mask 아이콘 팩**  
> 1,497개 기본 아이콘 세트 (총 2,983개 SVG), FontAwesome 호환 별칭 및 직관적인 카멜케이스(camelCase) 네이밍 완비.

---

## 🚀 주요 특징 (Key Features)

1. **초경량 스타일시트 (Ultra-lightweight)**
   - `fluenticons.min.css` 단 **81.7 KB** (Gzip 압축 시 약 **15.2 KB**)
   - 기존 176.8 KB 대비 **53.8% 용량 감축** 달성
   - 단일 CSS 규칙 및 경로 최적화(`i/{id}.svg`)로 최소 바이트 실현

2. **단일 클래스 & 2가지 모드 전환 (Fill & Solid)**
   - **기본값**: 채움형(Filled) 디자인 자동 적용 (`<i class="fi fiPlus"></i>`)
   - **솔리드 전환**: `.solid` 클래스만 붙이면 선형(Outline) 아이콘으로 즉시 전환 (`<i class="fi fiPlus solid"></i>`)

3. **엄격한 카멜케이스 & No-Hyphen 네이밍**
   - 중간바(`-`) 없는 직관적인 카멜케이스 적용 (`fiAccessCheck`, `fiUpload`, `fiSun`, `fiUser` 등)
   - 불필요하게 긴 클래스명을 대폭 줄여 HTML 마크업 바이트 절약

4. **FontAwesome 개발자 친화 별칭 (188+ Aliases)**
   - FontAwesome에서 널리 쓰이는 명칭(`fiPlus`, `fiTrash`, `fiUser`, `fiDownload`, `fiHeart`, `fiCheck`, `fiClose` 등)을 별칭으로 완벽 지원

5. **현대적인 CSS Mask 렌더링**
   - 웹폰트(Font)의 깜빡임(FOIT/FOUT)이나 렌더링 깨짐 현상 없음
   - 인라인 SVG처럼 HTML 코드가 비대해지지 않고 브라우저 캐싱 100% 활용
   - `color` (`currentColor`) 및 `font-size`에 자연스럽게 반응하며 호버/트랜지션 지원

---

## 📁 디렉터리 구성 (Directory Structure)

아이콘팩을 웹 프로젝트에 배포할 때 아래와 같이 폴더 구조를 유지합니다:

```text
fluenticons/
├── i/                      # SVG 아이콘 원본 파일 모음 (1.svg ~ 2983.svg)
├── fluenticons.min.css     # 프로덕션 배포용 압축 스타일시트 (81.7 KB)
├── fluenticons.css         # 개발 및 디버깅용 스타일시트 (99.6 KB)
├── index.html              # 실시간 아이콘 탐색기 (검색, 필터링, 클릭-복사)
├── icons.json              # 아이콘 메타데이터 JSON
├── icons.data.js           # 로컬 실행(file://) 지원용 정적 데이터
└── README.md               # 사용자 가이드 문서
```

> ⚠️ **주의**: `fluenticons.min.css`와 `i/` 폴더는 **반드시 동일한 디렉터리 레벨**에 위치해야 상대 경로(`url(i/{id}.svg)`)가 정상 작동합니다.

---

## 📦 빠른 시작 (Quick Start)

### 1. 스타일시트 불러오기

HTML 문서의 `<head>` 태그 안에 `fluenticons.min.css`를 추가합니다:

```html
<link rel="stylesheet" href="./lib/fluenticons/fluenticons.min.css" />
```

### 2. 아이콘 사용하기

기본 클래스 `fi`와 함께 원하는 아이콘 클래스를 지정합니다:

```html
<!-- 기본: 채움(Filled) 스타일 -->
<i class="fi fiPlus"></i>
<i class="fi fiUser"></i>
<i class="fi fiTrash"></i>
<i class="fi fiSun"></i>

<!-- 솔리드: 실선/선형(Solid/Regular) 스타일 (.solid 또는 .fiSolid) -->
<i class="fi fiPlus solid"></i>
<i class="fi fiUser solid"></i>
<i class="fi fiTrash solid"></i>
<i class="fi fiSun solid"></i>
```

---

## 🎨 스타일 커스터마이징 (Styling)

CSS Mask 기반으로 동작하므로 표준 텍스트 CSS 속성을 그대로 사용하여 크기와 색상을 조절할 수 있습니다:

### 크기 조절 (Sizing)

기본 크기는 `1em` × `1em`이므로 부모 폰트 크기를 따르거나 `font-size`로 조절합니다:

```html
<i class="fi fiUser" style="font-size: 16px;"></i>
<i class="fi fiUser" style="font-size: 24px;"></i>
<i class="fi fiUser" style="font-size: 2rem;"></i>
```

### 색상 조절 (Coloring)

아이콘은 부모 요소의 `color`(`currentColor`)를 상속받습니다:

```html
<!-- 인라인 스타일 -->
<i class="fi fiHeart" style="color: #ef4444;"></i>
<i class="fi fiCheckCircle" style="color: #10b981;"></i>

<!-- 버튼 또는 텍스트 컨테이너와 함께 사용 시 자동 색상 상속 -->
<button class="btn btnPrimary">
  <i class="fi fiUpload"></i> 파일 업로드
</button>
```

---

## 🔍 주요 아이콘 및 FontAwesome 매핑표

기존 FontAwesome 사용자가 쉽게 적응할 수 있도록 직관적인 별칭을 제공합니다:

| 용도 / 의미 | FluentIcons 클래스 (주요 별칭) | 선형 전환 예시 | FontAwesome 대응명 |
| :--- | :--- | :--- | :--- |
| **추가 / 플러스** | `fiPlus`, `fiAdd` | `<i class="fi fiPlus solid"></i>` | `fa-plus` |
| **원형 추가** | `fiPlusCircle`, `fiAddCircle` | `<i class="fi fiPlusCircle solid"></i>` | `fa-plus-circle` |
| **삭제 / 휴지통** | `fiTrash`, `fiDelete`, `fiDel` | `<i class="fi fiTrash solid"></i>` | `fa-trash`, `fa-trash-can` |
| **사용자 / 프로필** | `fiUser`, `fiPerson` | `<i class="fi fiUser solid"></i>` | `fa-user` |
| **팀 / 그룹** | `fiTeam`, `fiPeopleTeam`, `fiGroup` | `<i class="fi fiTeam solid"></i>` | `fa-users` |
| **체크 / 확인** | `fiCheck`, `fiCheckmark` | `<i class="fi fiCheck solid"></i>` | `fa-check` |
| **원형 체크** | `fiCircleCheck`, `fiCheckCircle` | `<i class="fi fiCircleCheck solid"></i>` | `fa-check-circle` |
| **닫기 / 취소** | `fiClose`, `fiTimes`, `fiDismiss`, `fiXmark`| `<i class="fi fiClose solid"></i>` | `fa-times`, `fa-xmark` |
| **원형 닫기** | `fiCircleXmark`, `fiDismissCircle` | `<i class="fi fiCircleXmark solid"></i>` | `fa-times-circle` |
| **업로드** | `fiUpload`, `fiArrUpload` | `<i class="fi fiUpload solid"></i>` | `fa-upload` |
| **다운로드** | `fiDownload`, `fiArrDownload` | `<i class="fi fiDownload solid"></i>` | `fa-download` |
| **클라우드 업로드** | `fiCloudUpload`, `fiCloudArrUp` | `<i class="fi fiCloudUpload solid"></i>` | `fa-cloud-upload-alt` |
| **달력** | `fiCalendar`, `fiCal`, `fiCalToday` | `<i class="fi fiCalendar solid"></i>` | `fa-calendar` |
| **태양 / 라이트** | `fiSun`, `fiWeatherSunny` | `<i class="fi fiSun solid"></i>` | `fa-sun` |
| **달 / 다크** | `fiMoon`, `fiWeatherMoon` | `<i class="fi fiMoon solid"></i>` | `fa-moon` |
| **문서 / 파일** | `fiDoc`, `fiFile`, `fiFileLines` | `<i class="fi fiDoc solid"></i>` | `fa-file`, `fa-file-alt` |
| **클립 / 첨부** | `fiClip`, `fiAttach`, `fiPaperclip` | `<i class="fi fiClip solid"></i>` | `fa-paperclip` |
| **알림 / 벨** | `fiBell`, `fiAlert` | `<i class="fi fiBell solid"></i>` | `fa-bell` |
| **수정 / 펜** | `fiPen`, `fiPencil`, `fiEdit` | `<i class="fi fiPen solid"></i>` | `fa-pen`, `fa-edit` |
| **복사** | `fiCopy`, `fiClone` | `<i class="fi fiCopy solid"></i>` | `fa-copy`, `fa-clone` |
| **붙여넣기** | `fiPaste`, `fiClipPaste` | `<i class="fi fiPaste solid"></i>` | `fa-paste` |
| **홈 / 집** | `fiHome`, `fiHouse` | `<i class="fi fiHome solid"></i>` | `fa-home`, `fa-house` |
| **새로고침 / 동기화**| `fiSync`, `fiRefresh`, `fiArrowsRotate`| `<i class="fi fiSync solid"></i>` | `fa-sync`, `fa-rotate` |
| **화살표(좌/우/상/하)**| `fiArrLeft`, `fiArrRight`, `fiArrUp`, `fiArrDown` | `<i class="fi fiArrLeft solid"></i>` | `fa-arrow-left` 등 |
| **꺾쇠(Chevron)** | `fiChevronLeft`, `fiChevronRight`, `fiChevronUp`, `fiChevronDown` | `<i class="fi fiChevronDown solid"></i>` | `fa-chevron-down` 등 |
| **검색** | `fiSearch`, `fiDocSearch` | `<i class="fi fiSearch solid"></i>` | `fa-search` |
| **와이파이** | `fiWifi`, `fiWifi4` | `<i class="fi fiWifi solid"></i>` | `fa-wifi` |
| **하트 / 좋아요** | `fiHeart` | `<i class="fi fiHeart solid"></i>` | `fa-heart` |
| **별 / 즐겨찾기** | `fiStar` | `<i class="fi fiStar solid"></i>` | `fa-star` |
| **로그인 / 진입** | `fiLogin`, `fiSignIn`, `fiSignInAlt` | `<i class="fi fiLogin solid"></i>` | `fa-sign-in-alt` |

---

## 💻 인터랙티브 아이콘 탐색기 (`index.html`)

아이콘 팩 루트에 포함된 `index.html` 파일을 웹 브라우저로 열면 편리한 전용 탐색기가 제공됩니다:

- **실시간 검색**: 카멜케이스명, FontAwesome 별칭, 영문 키워드로 즉시 검색
- **원클릭 복사**: 아이콘 카드를 클릭하면 즉시 `<i class="fi ..."></i>` HTML 코드가 클립보드에 복사
- **스타일 토글**: 상단 버튼을 통해 전체 아이콘의 "채움형(기본)"과 "솔리드형(solid)" 모드를 한눈에 비교
- **크기 및 테마 조절**: 28·36·48(기본)·64px 크기 변경 및 라이트/다크 테마 전환 지원
- **빠른 표시**: 카드는 120개씩 이어 붙이고(목록 끝이 보이면 다음 묶음), 아이콘 클래스는 카드가 화면 근처에 왔을 때만 붙여 보이는 아이콘의 SVG 만 받음 (화면 밖 카드는 `content-visibility: auto` 로 그리기 생략)
- **열기 방법**: 아이콘이 CSS mask 이미지라 브라우저 보안상 `file://` 로 직접 열면 SVG 가 막혀 보이지 않음 — 웹 서버(사이트의 `/lib/fluenticons/index.html`, VS Code Live Server 등)로 열 것

---

## ⚙️ 동작 원리 (Technical Mechanism)

이 아이콘팩은 CSS 마스크 속성과 CSS 변수를 결합한 초경량 설계를 적용하고 있습니다:

```css
/* 공통 마스크 베이스 */
.fi {
  display: inline-block;
  width: 1em;
  height: 1em;
  vertical-align: -0.125em;
  background-color: currentColor;
  -webkit-mask: var(--i) no-repeat 50% 50% / 100% 100%;
  mask: var(--i) no-repeat 50% 50% / 100% 100%;
  flex-shrink: 0;
  --i: var(--f, var(--r)); /* 기본값: 채움(Fill) 형 */
}

/* 솔리드 모드 전환 */
.fi.solid, .fi.fiSolid {
  --i: var(--r, var(--f)); /* 선형(Regular) 아이콘으로 대체 */
}

/* 개별 아이콘 정의 (단 1줄로 Fill과 Solid를 모두 지원) */
.fiAdd, .fiPlus { --f: url(i/8.svg); --r: url(i/7.svg); }
```

- 중복된 CSS 셀렉터 선언을 100% 배제하여 CSS 파일 크기를 혁신적으로 압축했습니다.
- 배경색은 `currentColor`로 지정되므로 텍스트 색상(`color`) 속성을 완벽하게 상속합니다.

---

## 📄 라이선스 (License)

- 원본 아이콘 디자인: [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) (MIT License) — 원본 저작권 고지와 라이선스 전문은 이 폴더의 `LICENSE.txt`(원문 그대로, 같은 폴더의 다른 라이브러리처럼 .txt)
- 아이콘팩 최적화 및 카멜케이스 매핑: **BitPlus Platform** (원본 고지를 유지한 수정본 — CSS mask 스타일시트, 카멜케이스 클래스 이름, FontAwesome 형 별칭)

원본 라이선스 전문 (`LICENSE.txt`):

```text
MIT License

Copyright (c) 2020 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
