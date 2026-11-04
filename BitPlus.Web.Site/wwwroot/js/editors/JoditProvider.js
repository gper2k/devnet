/** 글자 크기 목록(px) */
const FONT_SIZES = [9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 26, 28, 36, 48, 72];
/** 치환 태그 버튼 아이콘 (SunEditor 의 태그 버튼과 같은 대괄호 모양, 코드 보기 아이콘과 다름) */
const TAG_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 3H7a2 2 0 0 0-2 2v5a2 2 0 0 1-2 2 2 2 0 0 1 2 2v5a2 2 0 0 0 2 2h1"/><path d="M16 3h1a2 2 0 0 1 2 2v5a2 2 0 0 0 2 2 2 2 0 0 0-2 2v5a2 2 0 0 1-2 2h-1"/></svg>';
/** 치환 태그 항목을 HTML 로 안전하게 바꾼다 */
const escapeHtml = (text) => text.replace(/[&<>"]/g, c => `&#${c.charCodeAt(0)};`);
/**
 * Jodit 4 공급자 (wwwroot/lib/jodit). 이미지는 사이트 공용 업로드(api/system/editor/file)로 넣고(이미지 버튼, 파일 붙여넣기·끌어놓기),
 * 다른 소유자의 에디터 이미지를 붙여넣으면 현재 소유자로 복사한다. Jodit 고유 사항(툴바, 걸러내기 설정, iframe 편집 영역)은 모두 이 클래스 안에 둔다.
 * 번역은 jodit.min.js 에 모두 들어 있어 별도 언어 파일이 없다 (영어가 기본, 지원 언어는 languages 에 선언).
 */
export default class JoditProvider {
    name = "jodit";
    defaultPath = "/lib/jodit";
    /** 영어(내장 기본) 외에 제공하는 언어 (번역은 jodit.min.js 에 포함) */
    languages = ["ko"];
    assets(path, language) {
        return { css: [`${path}/jodit.min.css`], js: [`${path}/jodit.min.js`] };
    }
    content(path) {
        return { css: [], bodyClass: "jodit-wysiwyg" };
    }
    create(target, option, host, fill) {
        const origin = location.origin;
        // 글자 크기 목록(SunEditor 와 같게 13·15px 포함): 인스턴스 설정은 기본 목록과 위치별로 합쳐져 꼬리 항목(96)이 남으므로 기본 설정의 목록 자체를 바꾼다
        Jodit.defaultOptions.controls.fontsize.list = FONT_SIZES;
        const editor = Jodit.make(target, {
            language: host.language,
            theme: document.body.dataset.theme === "dark" ? "dark" : "default", // 사이트 다크 테마(body data-theme)면 Jodit 기본 다크 테마
            height: fill ? "100%" : (option.height ?? "20em"),
            minHeight: fill ? 200 : undefined,
            toolbarAdaptive: false,
            // 글자 크기: 기본 16px, 목록은 SunEditor 와 같게(13·15px 포함)
            style: { fontSize: "16px" },
            buttons: ["undo", "redo", "|", "paragraph", "fontsize", "|", "bold", "underline", "italic", "strikethrough", "|", "brush", "eraser", "|",
                "align", "ul", "ol", "lineHeight", "hr", "table", "|", "link", "image", ...(option.tags?.length ? [this.tagButton(option)] : []), "|", "source", "fullsize"],
            // 이미지 업로드(이미지 대화상자의 업로드 탭, 파일 붙여넣기·끌어놓기 모두): 서버로 직접 보내지 않고 사이트 공용 업로드(host.uploadImages)로 처리한다.
            // Jodit 가 업로드 결과로 받는 { success, data: { files, baseurl, isImages } } 형식으로 돌려주면 Jodit 가 이미지를 넣는다. 실패 알림은 공용 업로드가 이미 띄우므로 Jodit 쪽 오류 표시는 끈다.
            uploader: {
                insertImageAsBase64URI: false,
                url: "about:blank",
                customUploadFunction: async (formData) => {
                    const files = [...formData.values()].filter((v) => v instanceof File);
                    const items = await host.uploadImages(files, option);
                    return items
                        ? { success: true, data: { files: items.map(p => p.url), baseurl: "", path: "", isImages: items.map(() => true), messages: [] } }
                        : { success: false, data: { messages: [] } };
                },
                isSuccess: (resp) => !!resp?.success,
                process: (resp) => resp.data,
                getMessage: () => "",
                defaultHandlerError: () => { },
            },
            imageDefaultWidth: null, // 넣을 때 가로 크기를 강제하지 않는다 (SunEditor 와 같게 원본 크기, 크기는 에디터에서 조절)
            showPlaceholder: false, // 빈 본문에 "무엇이든 입력하세요" 같은 안내 문구를 보이지 않는다
            askBeforePasteHTML: false,
            askBeforePasteFromWord: false,
            defaultActionOnPaste: "insert_as_html",
            ...(option.siteCss ? { iframe: true, iframeCSSLinks: [`${origin}/css/site.min.css`, `${origin}/lib/fluenticons/fluenticons.min.css`] } : {}),
            // 걸러내기 끄기: 태그·이벤트 속성·스크립트·위험 링크 제거, iframe 샌드박스, 빈 요소 정리를 모두 끈다 (관리자가 쓰는 페이지 본문용)
            ...(option.rawHtml ? {
                cleanHTML: {
                    denyTags: false, allowTags: false, removeOnError: false, removeEventAttributes: false, safeJavaScriptLink: false, safeLinksTarget: false,
                    sandboxIframesInContent: false, convertUnsafeEmbeds: [], removeEmptyElements: false, fillEmptyParagraph: false, replaceNBSP: false
                }
            } : {}),
            ...option.options,
        });
        // 높이 100%: 부모(.editorFill)의 남는 높이를 채운다
        // 컨테이너를 세로 flex 로 바꾸고 작업 영역(툴바 아래)이 남는 높이를 채우게 한다 (Jodit 가 작업 영역에 고정 높이를 넣으므로 풀어 줌)
        if (fill) {
            Object.assign(editor.container.style, { flex: "1 1 auto", minHeight: "0", height: "auto", display: "flex", flexDirection: "column" });
            Object.assign(editor.workplace.style, { flex: "1 1 auto", minHeight: "0", height: "auto" });
        }
        // iframe 편집 영역(siteCss)은 사이트 CSS 를 따로 읽는 별도 문서라 body 의 data-theme 를 물려받지 못하므로, 사이트 테마를 iframe 문서(html)에도 붙여 다크 변수(바탕·글자색)가 적용되게 한다
        const theme = document.body.dataset.theme;
        if (theme && option.siteCss) {
            const applyTheme = (doc) => { if (doc?.documentElement)
                doc.documentElement.dataset.theme = theme; };
            editor.e.on("generateDocumentStructure.iframe", (doc) => applyTheme(doc));
            editor.waitForReady().then(() => applyTheme(editor.ed));
        }
        // 붙여넣기/끌어놓기 HTML 의 다른 소유자 이미지는 현재 소유자로 서버 복사하고 경로를 바꾼다
        editor.e.on(["afterPaste", "drop"], () => setTimeout(async () => {
            const html = await host.copyImages(editor.value, option);
            if (typeof html === "string")
                editor.value = html;
        }));
        return {
            native: editor,
            getHtml: () => editor.value,
            setHtml: html => { editor.value = html; },
            focus: () => editor.s.focus(),
            destroy: () => editor.destruct()
        };
    }
    /**
     * 치환 태그 삽입 드롭다운 버튼: 항목을 누르면 커서 위치에 태그 문자열을 넣는다 (IEditorOption.tags).
     * 툴바 버튼 목록 안에 객체로 넣고(이름 문자열로 넣으면 등록되지 않은 이름이 글자로 보임), 코드 보기(소스) 버튼과 구분되도록 전용 아이콘(대괄호)을 쓴다.
     */
    tagButton(option) {
        Jodit.modules.Icon.set("replaceTag", TAG_ICON);
        return {
            name: "replaceTag",
            tooltip: option.tagTitle ?? "Tag",
            icon: "replaceTag",
            list: Object.fromEntries(option.tags.map(p => [p.tag, `${p.tag}  ${p.name}`])),
            childExec: (editor, current, { control }) => editor.s.insertHTML(escapeHtml(String(control.name ?? ""))),
        };
    }
}
//# sourceMappingURL=JoditProvider.js.map