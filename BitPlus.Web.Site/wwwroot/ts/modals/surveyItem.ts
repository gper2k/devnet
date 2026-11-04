import IModal from "../IModal.js";

/**
 * 설문 항목 하나 (질문 + 옵션 + 선택 방식)
 */
export interface ISurveyItem {
    /** 질문 식별키 (새 항목은 빈 문자열, 서버가 정함) */
    id: string;
    /** 질문 글자 */
    text: string;
    /** true 면 여러 개 선택, false 면 하나만 선택 */
    multiple: boolean;
    /** 옵션 목록 (식별키는 새 옵션이면 빈 문자열) */
    options: { id: string; text: string }[];
}

/**
 * 설문 항목 입력 모달 인자 규격
 */
export interface ISurveyItemArgs {
    /** 고칠 항목 (없으면 새 항목) */
    item?: ISurveyItem;
    /** 옵션 최소 개수 */
    minOptions: number;
    /** 옵션 최대 개수 */
    maxOptions: number;
    /** 질문 최대 글자 수 */
    maxQuestion: number;
    /** 옵션 최대 글자 수 */
    maxOption: number;
}

/**
 * 설문 항목 입력 모달: common.modal.append("surveyItem", args) 로 열며, 질문, 선택 방식(하나/여러 개), 옵션(최소~최대 개, 처음에는 빈 옵션 최소 개수)을 입력받아 저장하면 항목(<see cref="ISurveyItem"/>)을, 취소하면 null 을 돌려준다.
 * 글쓰기 화면의 설문 편집기(Views/Content/Survey/Editor.cshtml)가 "설문 항목 추가"와 항목의 "편집"에서 연다.
 */
export default class SurveyItem implements IModal {
    private langs: Record<string, string>;

    /**
     * SurveyItem 기본 생성자
     */
    constructor() {
        this.langs = {};
    }

    /**
     * 문구 다국어 사전 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        this.langs = await window["common"].getLangsAsync("Cancel, Save, SurveyAdd, SurveyEditItem, SurveyQuestion, SurveyOptions, SurveyOptionAdd, SurveyMultiple, SurveyItemInvalid");
    }

    /**
     * 대화상자 DOM 생성 (제목 + 질문 + 선택 방식 + 옵션 목록 + 취소/저장)
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const common = window["common"];
        const t = this.langs;
        const a = args as ISurveyItemArgs;
        const diag = document.createElement("dialog");
        diag.className = "surveyItem";
        diag.innerHTML = `
            <header><i class="fi fiCheckmark"></i> <span>${a.item ? t["SurveyEditItem"] : t["SurveyAdd"]}</span></header>
            <div>
                <label>${t["SurveyQuestion"]}</label>
                <textarea name="text" rows="2" maxlength="${a.maxQuestion}"></textarea>
                <label class="check"><input type="checkbox" name="multiple" /><span>${t["SurveyMultiple"]}</span></label>
                <label>${t["SurveyOptions"]} (${a.minOptions}~${a.maxOptions})</label>
                <ul></ul>
                <button type="button" class="deco"><i class="fi fiAdd"></i> ${t["SurveyOptionAdd"]}</button>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${t["Cancel"]}</button>
                <button type="button" class="primary"><i class="fi fiSave"></i> ${t["Save"]}</button>
            </footer>
        `;
        const question = diag.querySelector<HTMLTextAreaElement>("textarea[name=text]")!;
        const multiple = diag.querySelector<HTMLInputElement>("input[name=multiple]")!;
        const list = diag.querySelector("ul")!;
        const addBtn = diag.querySelector<HTMLButtonElement>("div > button")!;
        const [cancelBtn, saveBtn] = diag.querySelectorAll<HTMLButtonElement>("footer > button");
        question.value = a.item?.text ?? "";
        multiple.checked = a.item?.multiple ?? false;

        /** 옵션 입력 줄 하나 (기존 옵션의 식별키는 data-id 에 보관) */
        const addOption = (text = "", id = "") => {
            const li = document.createElement("li");
            li.dataset["id"] = id;
            li.innerHTML = `<input type="text" maxlength="${a.maxOption}" autocomplete="off" /><button type="button" class="danger"><i class="fi fiDismiss"></i></button>`;
            li.querySelector("input")!.value = text;
            list.appendChild(li);
            sync();
        };
        /** 개수 한도에 맞춰 추가·삭제 버튼 상태를 맞춘다 */
        const sync = () => {
            addBtn.disabled = list.children.length >= a.maxOptions;
            list.querySelectorAll<HTMLButtonElement>("li > button").forEach(b => b.disabled = list.children.length <= a.minOptions);
        };
        (a.item?.options ?? []).forEach(o => addOption(o.text, o.id));
        while (list.children.length < a.minOptions) addOption();

        addBtn.addEventListener("click", () => { addOption(); list.querySelector<HTMLInputElement>("li:last-child input")!.focus(); });
        list.addEventListener("click", e => {
            const btn = (e.target as HTMLElement).closest("li > button");
            if (!btn || (btn as HTMLButtonElement).disabled) return;
            btn.closest("li")!.remove();
            sync();
        });
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        saveBtn.addEventListener("click", () => {
            const text = question.value.trim();
            const options = [...list.children].map(li => ({ id: (li as HTMLElement).dataset["id"] ?? "", text: li.querySelector("input")!.value.trim() })).filter(o => o.text);
            const invalid = !text || options.length < a.minOptions || new Set(options.map(o => o.text)).size !== options.length;
            if (invalid) return common.alert(t["SurveyItemInvalid"].replace("{0}", String(a.minOptions)).replace("{1}", String(a.maxOptions)));
            diag["closer"]({ id: a.item?.id ?? "", text, multiple: multiple.checked, options } as ISurveyItem);
        });
        requestAnimationFrame(() => question.focus());
        return diag;
    }
}
