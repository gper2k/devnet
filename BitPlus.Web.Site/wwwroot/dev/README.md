# wwwroot/dev

관리자 DevKit(`/Manager/DevKit`)에서 iframe 으로 보여주는 UI 샘플 HTML 모음. 순수 HTML 로 관리하며, 카탈로그 항목은 `Views/Manager/DevKit.cshtml` 의 `samples` 배열에 추가한다.

| 파일 | 내용 |
|---|---|
| `document.html` | 기본 스타일 (버튼/입력/토글/체크박스/inputGroup) |
| `toolbar.html` | 툴바 (플로팅/세그먼트/드롭다운/테마) |
| `treebox.html` | 트리뷰 (TreeBox) |
| `controls.html` | 입력 컨트롤 (태그 목록 `ul.tags`, 날짜·시간 컨트롤, 기간 설정 모달 사용법) |
| `editorfile.html` | 에디터 (Jodit/SunEditor 엔진 선택, `common.makeEditor` 2개 인스턴스: 이미지 업로드 후 url 삽입, 다른 소유자 이미지 붙여넣기/끌어놓기는 서버 복사 후 경로 교체, 미디어 제외), `common.renderContent` 샌드박스 출력·높이 자동 조정, 이미지 조회 헤더/년월 변조 404 확인 |
| `devkit.css` / `devkit.js` | 샘플 공용 스타일/스크립트 (copyCode 등) |

- `site.js` 동작(body 위임, `common.*`)이 필요한 샘플은 `<script type="module">` 에서 `new Site()` 로 생성한다.
