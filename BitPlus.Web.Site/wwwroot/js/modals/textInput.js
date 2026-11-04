/**
 * 여러 줄 글 한 개를 입력받는 공용 모달: common.textInput(content, options) 또는 common.modal.append("textInput", args) 로 열며,
 * 확인 시 입력한 글(앞뒤 공백 제거), 취소 시 null 로 닫힌다. 반려 사유처럼 짧은 의견 입력에 쓴다.
 */
export default class TextInput {
    langs;
    /**
     * TextInput 기본 생성자
     */
    constructor() {
        this.langs = {};
    }
    /**
     * 버튼 문구 다국어 사전 로드
     */
    async loadAsync(args) {
        this.langs = await window["common"].getLangsAsync("Cancel, Confirm");
    }
    /**
     * 대화상자 DOM 생성 (아이콘 제목 + 안내 문구 + 입력 칸 + 취소/확인)
     */
    render(args) {
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
        diag.querySelector(":scope > header > span").textContent = args["title"] ?? "";
        const message = diag.querySelector(":scope > div > p");
        message.textContent = args["content"] ?? "";
        message.hidden = !args["content"];
        const input = diag.querySelector("textarea");
        input.rows = Number(args["rows"]) || 4;
        input.maxLength = Number(args["maxLength"]) || 500;
        input.placeholder = args["placeholder"] ?? "";
        input.value = args["value"] ?? "";
        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        applyBtn.addEventListener("click", () => {
            const text = input.value.trim();
            if (args["required"] && !text)
                return input.focus(); // 필수 입력이 비어 있으면 닫지 않고 입력 칸으로
            diag["closer"](text);
        });
        requestAnimationFrame(() => input.focus());
        return diag;
    }
}
//# sourceMappingURL=textInput.js.map