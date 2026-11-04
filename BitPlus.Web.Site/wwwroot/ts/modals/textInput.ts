import IModal from "../IModal.js";

/**
 * 글 입력 모달 인자 규격
 */
export interface ITextInputArgs {
    /** 제목 (생략 시 안내 문구 없이 "Confirm" 계열 기본 제목은 쓰지 않고 비워 둠) */
    title?: string;
    /** 입력 위 안내 문구 (글자 그대로 출력) */
    content?: string;
    /** 입력 칸 안내 글자 */
    placeholder?: string;
    /** 처음 채울 값 */
    value?: string;
    /** 최대 글자 수 (기본 500) */
    maxLength?: number;
    /** true 면 비어 있으면 확인되지 않음 */
    required?: boolean;
    /** true 면 경고 색(반려 등 되돌리기 어려운 입력) */
    danger?: boolean;
    /** 제목 아이콘 클래스 (기본 fiEdit) */
    icon?: string;
    /** 입력 칸의 줄 수 (기본 4) */
    rows?: number;
    /** true 면 넓은 모양 (신고 내용처럼 긴 글 입력) */
    wide?: boolean;
}

/**
 * 여러 줄 글 한 개를 입력받는 공용 모달: common.textInput(content, options) 또는 common.modal.append("textInput", args) 로 열며,
 * 확인 시 입력한 글(앞뒤 공백 제거), 취소 시 null 로 닫힌다. 반려 사유처럼 짧은 의견 입력에 쓴다.
 */
export default class TextInput implements IModal {
    private langs: Record<string, string>;

    /**
     * TextInput 기본 생성자
     */
    constructor() {
        this.langs = {};
    }

    /**
     * 버튼 문구 다국어 사전 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        this.langs = await window["common"].getLangsAsync("Cancel, Confirm");
    }

    /**
     * 대화상자 DOM 생성 (아이콘 제목 + 안내 문구 + 입력 칸 + 취소/확인)
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const diag = document.createElement("dialog");
        diag.className = "inputWin" + (args["danger"] ? " danger" : "") + (args["wide"] ? " wide" : "");
        diag.innerHTML = `
            <header><i class="fi ${args["icon"] || "fiEdit"}"></i> <span></span></header>
            <div>
                <p></p>
                <textarea rows="4"></textarea>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${this.langs["Cancel"]}</button>
                <button type="button" class="primary"><i class="fi fiCheckmark"></i> ${this.langs["Confirm"]}</button>
            </footer>
        `;
        diag.querySelector(":scope > header > span")!.textContent = args["title"] ?? "";
        const message = diag.querySelector<HTMLParagraphElement>(":scope > div > p")!;
        message.textContent = args["content"] ?? "";
        message.hidden = !args["content"];
        const input = diag.querySelector<HTMLTextAreaElement>("textarea")!;
        input.rows = Number(args["rows"]) || 4;
        input.maxLength = Number(args["maxLength"]) || 500;
        input.placeholder = args["placeholder"] ?? "";
        input.value = args["value"] ?? "";

        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        applyBtn.addEventListener("click", () => {
            const text = input.value.trim();
            if (args["required"] && !text) return input.focus(); // 필수 입력이 비어 있으면 닫지 않고 입력 칸으로
            diag["closer"](text);
        });
        requestAnimationFrame(() => input.focus());
        return diag;
    }
}
