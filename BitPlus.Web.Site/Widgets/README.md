# BitPlus.Web.Site/Widgets (위젯 페이지)

Home(`/`)·컨텐츠 포탈(`/content`) 같은 **위젯 페이지**에 영역(Section)을 위에서 아래로 배치하고, 영역의 칸(Slot)마다 위젯을 넣는 기능의 서버 코드입니다.

- 위젯 = `Views/Widgets/{키}/` 폴더 (widget.json + View.cshtml + Edit.cshtml), 영역 = `Views/Sections/{키}/section.json`, 페이지 정의 = `Views/Sections/_Pages/{페이지}.json` (각 폴더 README 참고).
- 위젯·영역 정보는 사이트 시작 때 폴더 JSON 을 읽어 `WidgetTypes`·`SectionTypes` 테이블과 맞춥니다(새 폴더 = 자동 추가).
- 페이지 배치는 설정 `PageLayout.{페이지}` 에 JSON 으로 저장합니다(없으면 페이지 정의의 기본 배치).
- 크기 단위 = 가로 4칸 격자의 칸 수 × 행 수. 위젯 최소·최대를 1024 이상 배치의 슬롯 크기와 비교합니다.

| 파일 | 역할 |
| :--- | :--- |
| `WidgetCatalog.cs` | 폴더 JSON ↔ 정보 테이블 동기화 (없으면 추가, version 이 크면 갱신·사용 여부 유지, 사라지면 Available=false, 잘못된 JSON 은 건너뛰고 로그). 키 형식 `KeyPattern` |
| `SectionGrid.cs` | 영역 정의 → 화면 폭 4단계(1024 이상·1024 미만·780 미만·520 미만) 영역 줄, 슬롯 목록·크기, grid CSS(미디어 쿼리). 가로형(`repeat`)은 칸 수로 줄을 만듦 |
| `PageLayouts.cs` | 페이지 정의 읽기(카테고리·기본 배치), 배치 읽기·저장(설정 `PageLayout.{페이지}`), 카테고리 허용 판정 |
| `PageLayoutValidator.cs` | 저장할 배치 검사·정리 (영역 id·종류, 칸 수, 슬롯, 위젯 종류·카테고리·크기, 제목, 설정값) |
| `WidgetSettings.cs` | widget.json settings(number·bool·text·lines·html·board·boards)에 맞춰 설정값 정리 (기본값, 형식, HTML 정제) |
| `PageSectionReader.cs` | 페이지 영역 목록 만들기(요청 단위 서비스) — 영역/슬롯, 제목(다국어), 설정값, 데이터 로더 결과. 한계: 캐시 없음 |
| `SectionView.cs`, `SlotView.cs` | 그릴 영역·슬롯 정보 (`SlotView` = 위젯 출력 뷰 모델, `Lines(key)` = 줄마다 `\|` 로 나눈 값, `SafeUrl` = `/…`·http(s) 주소만, `IconOf` = fi 아이콘 이름 검사, `ThemeSvg` = 레이아웃 색을 따르는 SVG 를 문서에 넣을 태그로) |
| `IWidgetLoader.cs`, `WidgetContext.cs` | 위젯 데이터 로더(선택, 위젯 키별) 인터페이스와 넘기는 정보 |
| `BoardWidgetLoader.cs` | 게시판 위젯 로더 공통 (읽을 수 있는 게시판의 원본 글, `BoardAccess.CanOpen` 통과한 것만, 다국어 게시판 글은 현재 언어 번역의 제목·요약 — 없으면 원문) |
| `LatestPostsLoader.cs`, `NoticeLoader.cs`, `GalleryLoader.cs` | 최신 글·공지(공지글만)·갤러리(대표 이미지) 로더 |
| `WidgetPost.cs` | 게시판 위젯 글 한 줄 (제목·주소·게시판·작성일·요약·이미지) |
| `CultureText.cs` | 문화권별 JSON 객체에서 표시 글자 고르기 (다국어 출력 규칙) |
| `WidgetServiceExtend.cs` | `AddWidgets()`(읽기 서비스 + 로더 등록), `SyncWidgetsAsync()`(시작 때 동기화) — Program.cs, 설치 후에만 |

## 새 위젯 만들기

1. `Views/Widgets/{키}/widget.json` (key, version, name, description, categories, size.min/max, settings), `View.cshtml`(모델 `SlotView`), `Edit.cshtml`(Layout = null, 입력 name = 설정 키).
2. 데이터가 필요하면 `IWidgetLoader` 구현 클래스(Widget = 키)를 추가 — 자동 등록.
3. 스타일은 `View.cshtml.scss` + compilerconfig 등록. 사이트를 다시 시작하면 목록에 나옵니다.
