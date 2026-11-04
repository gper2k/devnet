# SunEditor 3.3.3

WYSIWYG 에디터 라이브러리 (npm `suneditor@3.3.3`, MIT — `LICENSE.txt`).

| 파일 | 원본 경로 | 용도 |
|------|-----------|------|
| `suneditor.min.js` | `dist/` | 에디터 본체 |
| `suneditor.min.css` | `dist/` | 에디터 UI 스타일 |
| `suneditor-contents.min.css` | `dist/` | 본문 출력 스타일 (`site.ts` renderContent) |
| `ko.js` | `src/langs/` | 한국어 언어팩 (`window.SUNEDITOR_LANG.ko`) |

참조: `wwwroot/dev/editorfile.html`, `wwwroot/ts/site.ts`.
업데이트: `npm pack suneditor@<ver>` 후 위 파일만 교체.
