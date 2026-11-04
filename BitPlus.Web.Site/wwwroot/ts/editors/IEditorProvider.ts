/**
 * 에디터 공용 규격: 에디터 엔진(SunEditor 등)마다 한 개의 공급자(IEditorProvider)를 두고,
 * 사이트(common)가 에셋 로딩·소유자/이미지 업로드 연동·호출 방식을 공용으로 처리한다.
 * 호출하는 쪽(뷰)은 엔진을 모르고 common.makeEditor(textarea, option) 와 반환값(IEditor)만 쓴다.
 */

/** common.makeEditor 옵션 (소유자/암호는 업로드 때마다 읽으므로 getter 로 줄 수 있다). 엔진에 상관없는 값만 두고, 엔진 고유 값은 options 로 넘긴다. */
export interface IEditorOption {
    ownerType: string;                                              // 소유자 형식 (32자 이내, 예: Content)
    ownerKey: string;                                               // 소유자 식별키 (64자 이내, 예: 글 GUID)
    password?: string;                                              // 비인증 등록 암호
    height?: string;                                                // 에디터 높이 (기본 "100%": 부모 남는 높이를 채움(부모에 .editorFill 추가, 최소 200px), 그 외 값은 고정 높이)
    onFiles?: (kind: "upload" | "copy", data: any) => void;         // 업로드/붙여넣기 복사 결과 콜백
    siteCss?: boolean;                                              // true 이면 편집 영역에 사이트 CSS(site.min.css, 아이콘)를 연결해 보기 화면과 같은 스타일로 편집
    tags?: { tag: string; name: string }[];                         // 치환 태그 목록 (있으면 툴바에 태그 삽입 버튼 추가, 예: {#userid})
    tagTitle?: string;                                              // 태그 삽입 버튼 제목
    rawHtml?: boolean;                                              // true 이면 엔진의 태그·속성·스타일 걸러내기를 끄고 코드 보기도 자유롭게 둔다 (관리자가 쓰는 페이지 본문용)
    options?: Record<string, any>;                                  // 엔진 고유 옵션 (공급자가 그대로 병합, 엔진을 바꾸면 의미가 달라지므로 꼭 필요한 곳에만)
}

/** 에디터 인스턴스: 호출하는 쪽이 쓰는 최소 공통 동작 */
export interface IEditor {
    /** 엔진 원본 인스턴스 (엔진 고유 기능이 꼭 필요할 때만 사용) */
    readonly native: any;
    /** 본문 HTML */
    getHtml(): string;
    /** 본문 HTML 교체 */
    setHtml(html: string): void;
    /** 편집 영역에 포커스 */
    focus(): void;
    /** 에디터 제거 (원래 textarea 로 되돌림) */
    destroy(): void;
}

/** 사이트가 공급자에게 빌려주는 공용 기능 (엔진마다 이미지 삽입 방식만 다르고 서버 연동은 같다) */
export interface IEditorHost {
    /** 에디터 UI 언어: 공급자가 지원하면 현재 UI 문화권(예: "ko"), 아니면 기본인 영어("en") — 이미 정해진 값 */
    language: string;
    /** 이미지 파일을 소유자(option)로 서버에 업로드하고 결과 [{ url, name, size }] 를 돌려준다 (실패하면 null) */
    uploadImages(files: File[], option: IEditorOption): Promise<{ url: string; name: string; size: number }[] | null>;
    /** 붙여넣기/끌어놓기 HTML 의 다른 소유자 에디터 이미지를 현재 소유자로 복사하고 경로를 바꾼 HTML 을 돌려준다 (이미지가 없으면 undefined, 실패하면 false) */
    copyImages(html: string, option: IEditorOption): Promise<string | boolean | void>;
}

/** 에디터를 쓰려면 페이지에 로드해야 하는 파일 */
export interface IEditorAssets {
    css: string[];
    js: string[];
}

/** 에디터 엔진 공급자: 새 에디터는 이 규격으로 클래스 하나를 만들어 Site 생성자에서 등록하면 된다. */
export interface IEditorProvider {
    /** 공급자 이름 (ISiteOptions.editor 값, 예: "suneditor") */
    readonly name: string;
    /** 엔진 파일 기본 경로 (ISiteOptions.editorPath 를 주지 않았을 때, 예: "/lib/suneditor") */
    readonly defaultPath: string;
    /**
     * 영어(기본, 에디터 자체 내장)에 더해 이 공급자가 제공하는 언어 코드 (예: ["ko"]). 현재 UI 문화권이 여기 없으면 영어로 동작합니다.
     * 어떤 에디터든 영어를 기본으로 두고, 한국어 등을 지원할 수 있으면 여기에 선언하고 assets 에서 그 언어 파일을 로드합니다.
     */
    readonly languages: readonly string[];
    /**
     * 에디터 생성에 필요한 CSS/JS (js 는 순서대로 로드)
     * @param path 엔진 파일 경로 (끝에 / 없음)
     * @param language 에디터 UI 언어 ("en" 이면 기본 영어라 언어 파일 불필요, 그 외는 languages 중 하나)
     */
    assets(path: string, language: string): IEditorAssets;
    /**
     * 에디터로 만든 본문을 보기 화면에 보일 때 필요한 것: 본문 스타일 CSS 와 본문 요소에 붙일 클래스
     * @param path 엔진 파일 경로 (끝에 / 없음)
     */
    content(path: string): { css: string[]; bodyClass: string };
    /**
     * textarea 를 에디터로 바꿉니다 (assets 가 로드된 뒤 호출됨). 높이 100%(fill) 처리와 이미지 업로드/복사 연동도 여기서 합니다.
     * @param target 에디터로 바꿀 textarea
     * @param option 공용 옵션
     * @param host 사이트 공용 기능 (이미지 업로드/복사)
     * @param fill 높이가 "100%"(부모 남는 높이 채움)인지 여부
     */
    create(target: HTMLTextAreaElement, option: IEditorOption, host: IEditorHost, fill: boolean): IEditor;
}
