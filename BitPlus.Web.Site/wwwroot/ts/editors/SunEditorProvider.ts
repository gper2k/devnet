import { IEditor, IEditorAssets, IEditorHost, IEditorOption, IEditorProvider } from "./IEditorProvider.js";

/** 페이지에 로드된 SunEditor 전역 (lib/suneditor/suneditor.min.js) */
declare const SUNEDITOR: any;

/**
 * 치환 태그 삽입 드롭다운 플러그인 클래스 생성 (IEditorOption.tags). 목록 항목을 누르면 커서 위치에 태그 문자열을 넣는다.
 * 항목은 에디터 옵션 replaceTag.items([{ tag, name }])로 받는다.
 * @param title 툴바 버튼 제목
 * @returns SunEditor 플러그인 클래스
 */
function makeTagPlugin(title: string): any {
    const html = (text: string) => text.replace(/[&<>"]/g, c => `&#${c.charCodeAt(0)};`);
    return class extends SUNEDITOR.interfaces.PluginDropdown {
        static key = "replaceTag";
        static className = "";
        constructor(kernel: any, pluginOptions: { items?: { tag: string; name: string }[] }) {
            super(kernel);
            this.title = title;
            this.icon = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 3H7a2 2 0 0 0-2 2v5a2 2 0 0 1-2 2 2 2 0 0 1 2 2v5a2 2 0 0 0 2 2h1"/><path d="M16 3h1a2 2 0 0 1 2 2v5a2 2 0 0 0 2 2 2 2 0 0 0-2 2v5a2 2 0 0 1-2 2h-1"/></svg>';
            this.$.menu.initDropdownTarget(this.constructor, (pluginOptions.items ?? []).map(p => ({
                command: "tag", value: html(p.tag), title: html(p.name), innerHTML: `<code>${html(p.tag)}</code> ${html(p.name)}`
            })), { className: "se-list-tag" });
        }
        /** 목록 항목 선택: 태그 문자열을 커서 위치에 넣고 목록을 닫는다 */
        action(target: HTMLElement) {
            this.$.html.insert(html(target.getAttribute("data-value") ?? ""));
            this.$.menu.dropdownOff();
        }
    };
}

/**
 * SunEditor 3 공급자 (wwwroot/lib/suneditor). 이미지는 사이트 공용 업로드(api/system/editor/file)로 넣고 미디어 기능은 제외한다.
 * SunEditor 고유 사항(툴바, 글자 크기/줄 높이 목록, strictMode, iframe 편집 영역)은 모두 이 클래스 안에 둔다.
 */
export default class SunEditorProvider implements IEditorProvider {
    readonly name = "suneditor";
    readonly defaultPath = "/lib/suneditor";
    /** 영어(내장 기본) 외에 언어 파일(예: ko.js)을 제공하는 언어 */
    readonly languages = ["ko"];

    /** SunEditor 본문(비 iframe)을 감싸는 클래스: 보기 화면 본문 요소에 붙이는 이름 */
    private static readonly bodyClass = "sun-editor-editable";

    assets(path: string, language: string): IEditorAssets {
        return {
            css: [`${path}/suneditor.min.css`],
            js: [`${path}/suneditor.min.js`, ...(this.languages.includes(language) ? [`${path}/${language}.js`] : [])]
        };
    }

    content(path: string) {
        return { css: [`${path}/suneditor-contents.min.css`], bodyClass: SunEditorProvider.bodyClass };
    }

    create(target: HTMLTextAreaElement, option: IEditorOption, host: IEditorHost, fill: boolean): IEditor {
        const tags = option.tags?.length ? { replaceTag: makeTagPlugin(option.tagTitle ?? "Tag") } : {};
        const origin = location.origin;
        let pending: string | null = null; // iframe 편집 영역은 비동기로 준비되므로 그 전에 setHtml 하면 준비된 뒤에 넣는다
        const editor = SUNEDITOR.create(target, {
            lang: window["SUNEDITOR_LANG"]?.[host.language], // 영어는 내장 기본이라 undefined
            width: "100%",
            height: option.height ?? "100%",
            plugins: { ...SUNEDITOR.plugins, ...tags },
            ...(option.tags?.length ? { replaceTag: { items: option.tags } } : {}),
            excludedPlugins: ["video", "audio", "embed", "math", "drawing", "fileUpload", "fileGallery", "fileBrowser", "imageGallery", "videoGallery", "audioGallery", "exportPDF", "layout", "template"],
            buttonList: [
                ["undo", "redo"],
                ["blockStyle", "fontSize"],
                ["bold", "underline", "italic", "strike"],
                ["fontColor", "backgroundColor", "removeFormat"],
                ["align", "list", "lineHeight", "hr", "table"],
                ["link", "image", ...Object.keys(tags)],
                ["codeView", "fullScreen"],
            ],
            image: { allowMultiple: true, acceptedFormats: "image/*" },
            // 글자 크기(px): 기본 목록의 9~16px 구간을 1px 간격으로 (기본 8, 10, 13, 15 → 9, 10, 11, 12, 13, 14, 15, 16), 그 위 크기는 그대로
            // 줄 높이: 기본 목록(1, 1.2, 1.7, 2)에서 1.7 을 빼고 1.2 와 2 사이에 1.4, 1.6, 1.8 추가
            lineHeight: { items: [1, 1.2, 1.4, 1.6, 1.8, 2].map(v => ({ text: String(v), value: `${v}em` })) },
            fontSize: { unitMap: { px: { default: 16, inc: 1, min: 8, max: 72, list: [9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 26, 28, 36, 48, 72] } } },
            ...(option.siteCss ? { iframe: true, iframe_cssFileName: [`${origin}/css/site.min.css`, `${origin}/lib/fluenticons/fluenticons.min.css`] } : {}),
            // 걸러내기 끄기: 태그·속성·클래스·스타일 필터를 모두 끄고 코드 보기도 자유롭게 (관리자가 쓰는 페이지 본문용)
            ...(option.rawHtml ? { freeCodeViewMode: true, strictMode: { tagFilter: false, textStyleTagFilter: false, classFilter: false, styleFilter: false, formatFilter: false, attrFilter: false } } : {}),
            ...option.options,
            events: {
                onImageUploadBefore: ({ $, info }) => this.upload($, info, option, host),
                onPaste: ({ data }) => host.copyImages(data, option),
                onDrop: ({ data }) => host.copyImages(data, option),
                ...option.options?.events,
                onload: (arg: any) => {
                    if (pending !== null) editor.$.html.set(pending);
                    pending = null;
                    return option.options?.events?.onload?.(arg);
                },
            },
        });
        // 높이 100%: 에디터가 루트에 인라인 display:block 을 넣으므로 세로 flex 로 바꿔 편집 영역이 남는 높이를 채우게 한다 (.editorFill 스타일)
        if (fill) target.parentElement!.querySelector<HTMLElement>(":scope > .sun-editor")!.style.display = "flex";
        return {
            native: editor,
            getHtml: () => editor.$.html.get(),
            setHtml: html => {
                try { editor.$.html.set(html); pending = null; } catch { pending = html; }
            },
            focus: () => editor.$.focusManager.focus(),
            destroy: () => editor.destroy()
        };
    }

    /** 이미지 업로드: 공용 업로드 결과의 url 로 이미지 플러그인에 넣고 기본 업로드는 취소한다 (외부 URL 삽입은 그대로) */
    private async upload($: any, info: any, option: IEditorOption, host: IEditorHost): Promise<boolean | void> {
        if (info.url) return true;
        const items = await host.uploadImages([...info.files], option);
        if (!items) return false;
        for (const item of items) $.plugins.image.uploadService.urlUpload({ ...info, url: item.url, files: { name: item.name, size: item.size } });
    }
}
