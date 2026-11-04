# BitPlus.Web.Site wwwroot/ts (클라이언트 TypeScript)

빌드 시 `npx tsc -p tsconfig.json` 로 `wwwroot/js` 에 컴파일됩니다. 전역 객체 `common`(`site.ts` 의 `Site` 인스턴스)으로 공용 기능을 제공합니다.

## 파일 구성

| 파일 | 역할 |
| :--- | :--- |
| `site.ts` | 전역 `common` — Ajax(`getSend`/`postSend`/`putSend`/`patchSend`/`delSend`), 메시지 박스(`alert`/`info`/`confirm`), 다국어 사전(`getLangsAsync`), 트리 생성(`makeTree`), 폼 데이터(`makeFormData`), 다국어 입력(`langInput`/`getLang`/`setLang`), 키-값 속성(`setProps`/`getProps`), 칩 목록(`setChips`/`getChips`), 빈 데이터 항목(`syncEmpty`), 드래그 순서 변경(`bindDragSort`), 공용 에디터(`makeEditor`: SunEditor 생성, 이미지 업로드·다른 소유자 이미지 붙여넣기 복사 처리, 미디어 제외), 본문 샌드박스 출력(`renderContent`: srcdoc iframe, allow-same-origin 없음, 높이 자동 조정), 페이저(`makePager`) 및 body 전역 이벤트 위임 |
| `editors/IEditorProvider.ts` | 에디터 공용 규격 — `IEditorOption`(소유자·높이·`siteCss`·`tags`·`rawHtml`·엔진 고유 `options`), `IEditor`(`getHtml`/`setHtml`/`focus`/`destroy`/`native`), `IEditorHost`(공용 이미지 업로드·복사), `IEditorProvider`(엔진 하나) |
| `editors/SunEditorProvider.ts` | SunEditor 3 공급자 — 엔진 파일(`/lib/suneditor`), 툴바·글자 크기·줄 높이·치환 태그 플러그인·iframe 편집 영역·걸러내기 해제 등 SunEditor 고유 사항은 모두 여기 |
| `editors/JoditProvider.ts` | Jodit 4 공급자 — 엔진 파일(`/lib/jodit`, 번역 포함이라 언어 파일 없음), 툴바·치환 태그 버튼·iframe 편집 영역·`rawHtml` 걸러내기 해제(`cleanHTML` 설정)·공용 이미지 업로드/붙여넣기 복사 연결 |
| `TreeBox.ts` | 폴더 트리 컨트롤 — `render`(같은 영역 재바인딩 시 열림/선택 유지), `upsert`/`remove`/`refresh`, 체크 목록(`getChecked`/`getCheckedNodes`/`setChecked`), 옵션 `noMenuKeys`(확장메뉴 제외 키), 노드 `name`(순수 이름 — 있으면 `text` 를 HTML 로 그려 뱃지 등을 붙일 수 있음, 툴팁·정렬·`getNode` 조회용) |
| `PopMenu.ts` | 기준 요소 옆 확장 메뉴(`showPopMenu`/`closePopMenu`, `.treeMenu` 스타일) — TreeBox 확장 메뉴와 `common.popMenu(anchor, items)`(목록 행 ⋮ 버튼 등)가 공용 사용 |
| `ColorHelper.ts` | 서비스 컬러선택 도우미(/Service/Color) — 색 세트 편집(3~7색, 색 선택기 팝오버·스포이드), 브라우저 IndexedDB 프리셋, 샘플 슬롯(main·sub·point·deco) 선택기로 샘플 영역 색 변수(`--x`/`Light`/`Dark`/`1~9`) 지정, 명도 대비 목록, CSS 변수 코드·preset.json 내보내기/가져오기 |
| `IconBrowser.ts` | 서비스 아이콘 페이지(/Service/Icons) — icons.json(브라우저 캐시) 메타데이터로 메모리 검색(디바운스 150ms), 180개씩 이어 붙이기(끝 감시 IntersectionObserver), 화면 근처 칸에만 아이콘 클래스 부여(SVG 지연 로드), 채움/선형·크기, 클릭 시 코드 복사 |
| `modalManager.ts` | `common.modal.append(name, args)` — `modals/{name}.js` 동적 로드, `closer(값)` 으로 닫으면 값(객체 포함)을 그대로 반환 |
| `validresult.ts` | 검증 결과 — `popover()`(토스트), `markErrors(영역)`(오류 입력항/입력 그룹에 `error` 표시), `clearErrors`/`clearError` |
| `validator.ts` | 서버와 같은 `ValidConfigs/*.json` 으로 클라이언트 검증. `valid(values, container?)` 에 영역을 주면 같은 name 입력항의 `data-min`/`data-max` 로 길이 규칙 대체 |
| `modals/` | 공용 모달 (README 참고) |

## body 전역 이벤트 위임 (site.ts)
- `input[data-lang]` 직접 입력 → JSON 의 현재 문화권 값 갱신, `input[data-lang] + button` → 다국어 입력 모달(`data-title`, `@키` 는 다국어 조회)
- `.keyValueBox > ul > li > button` 삭제, `.keyValueBox > footer > button` 추가
- `.chipBox > ul > li > button` 삭제, `.chipBox > footer > button` → `data-modal` 선택 모달 (기본: 기존 항목 선택 불가 + 추가, `data-sync`: 기존 항목 체크 상태로 열어 선택 결과로 동기화). 기본 지정이 없으면 첫 항목 자동 지정
- `ul.pager > li[data-page]` → ul 에 `page` 이벤트(`makePager().then(cb)` 로 수신)
- `ul.tags > li > input` 엔터(한글 조합 중이면 조합 완료 후)/change → 입력값을 입력항 `li` 앞에 태그로 추가하고 입력 비움(빈 값·대소문자 무시 중복 제외), `ul.tags > li:not(:has(> input)) > button` 태그 삭제, 빈 영역 클릭 시 입력항 포커스. 추가/삭제 시 ul 에 `tag` 이벤트(`detail: { text, add }`). 값은 `common.getTags(list)` / `common.setTags(list, tags)`
- 모든 `dialog` 의 open 변화를 감시해 중첩 상태 속성(`data-first`/`data-under`) 부여

## 에디터 교체 방법
뷰는 엔진을 모릅니다: `const editor = await common.makeEditor(textarea, option)` 와 `editor.getHtml()`/`setHtml()` 만 씁니다. 엔진 파일(CSS/JS)은 `common` 이 공급자에 맞게 한 번만 알아서 로드하므로 뷰에 `<link>`/`<script>` 를 넣지 않습니다. 에디터로 만든 본문을 에디터 없이 보여 주는 페이지는 `common.loadEditorAssets(true)`(본문 스타일 CSS 만), 샌드박스 출력은 `common.renderContent`(공급자의 content CSS·본문 클래스 사용).
- **등록된 에디터**: `suneditor`(`/lib/suneditor`), `jodit`(기본, `/lib/jodit`). 사이트 전체 에디터는 관리자 "시스템 설정 > 사이트 정보" 의 **주 에디터**로 고릅니다(`SiteInfo.Editor` → `_Layout.cshtml` 이 `new Site({ editor })` 로 넘김, 선택지는 `common.editorNames`). 등록되지 않은 이름이나 빈 값이면 기본 에디터를 씁니다. 코드에서 바꾸려면 `new Site({ editor: "jodit" })`, 또는 경로만 `new Site({ editorPath: "/lib/jodit" })` — 이름을 주지 않으면 경로 마지막 폴더 이름이 공급자 이름으로 시작하는 공급자를 씁니다(`/lib/jodit` → jodit, `/lib/suneditor-4` → suneditor). 기본값은 `Site` 생성자의 이름(현재 `"jodit"`).
- **같은 엔진의 다른 경로/버전**: `new Site({ editorPath: "/lib/suneditor-4" })` (또는 `Site` 생성자의 기본값 변경).
- **다른 에디터**: `IEditorProvider` 를 구현한 클래스(`editors/XxxProvider.ts`: `name`, `defaultPath`, `assets`, `content`, `create`)를 만들어 `Site` 생성자의 `registerEditor(new XxxProvider())` 줄에 등록하고 `editor: "xxx"`(기본값은 생성자의 `"suneditor"`)만 바꾸면 모든 화면이 바뀝니다. 서버 연동(이미지 업로드/붙여넣기 복사, 소유자/암호)은 `IEditorHost` 로 공용 처리되니 공급자는 엔진 쪽 이벤트만 연결하면 됩니다.
- **언어**: 영어가 기본(에디터 내장)이고, 현재 UI 문화권(`ko`, `ko-KR` 도 `ko` 로)을 공급자가 지원하면 그 언어를 씁니다. 공급자는 영어에 더해 제공하는 언어를 `languages`(예: SunEditor `["ko"]`)에 선언하고 `assets(path, language)` 에서 그 언어 파일을 로드하며, `Site.editorLanguage` 가 정한 언어(`host.language`)로 엔진 언어를 지정합니다. 지원하지 않는 문화권은 영어로 동작합니다. 다른 에디터를 추가할 때도 같은 규칙(영어 기본, 한국어 등 지원 가능하면 선언)을 따릅니다.
- **위치마다의 특이 사항**은 엔진에 상관없는 옵션으로 표현합니다: `height`, `siteCss`(사이트 CSS 연결), `tags`/`tagTitle`(치환 태그), `rawHtml`(걸러내기 끔), `onFiles`, `ownerType`/`ownerKey`/`password`. 엔진 고유 설정이 꼭 필요하면 `options` 로 넘기되 엔진을 바꾸면 달라집니다. 새 옵션이 필요하면 `IEditorOption` 에 추가하고 공급자가 해석합니다.
- 본문 영역 스타일(`wwwroot/css/_Editor.scss` 의 `sun-editor-editable`)은 SunEditor 본문 클래스에 묶여 있으니, 엔진을 바꾸면 공급자 `content().bodyClass` 에 맞춰 함께 손봅니다.
