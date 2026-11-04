/**
 * 삭제 등 되돌릴 수 없는 작업 전에 추가 확인을 받는 경고창 (일반 메시지 창과 별개).
 * common.checkConfirm(content, items, checked, title) 또는 common.modal.append("checkConfirm", args) 로 열며,
 * 확인 시 체크된 옵션 키 배열(옵션이 없으면 빈 배열), 취소 시 null 로 닫힌다.
 */
export default class CheckConfirm {
    langs;
    /**
     * CheckConfirm 기본 생성자
     */
    constructor() {
        this.langs = {};
    }
    /**
     * 버튼 문구와 인자 중 "@" 다국어 키 문구를 한 번에 로드
     */
    async loadAsync(args) {
        const texts = [args["title"] || "@Confirm", args["content"] ?? "", ...Object.values(args["items"] ?? {}).map(CheckConfirm.itemText)];
        const keys = ["Cancel", "Confirm", ...texts.filter(p => p.startsWith("@")).map(p => p.slice(1))];
        this.langs = await window["common"].getLangsAsync(keys.join(","));
    }
    /**
     * 대화상자 DOM 생성 (물음표 아이콘 제목 + 본문 + 체크 옵션 + 취소/확인)
     */
    render(args) {
        const items = args["items"] ?? {};
        const checked = [...(args["checked"] ?? []), ...Object.keys(items).filter(key => items[key]?.checked === true)];
        const diag = document.createElement("dialog");
        diag.className = "checkConfirm";
        diag.innerHTML = `
            <header><i class="fi fiChatHelp"></i> <span></span></header>
            <div>
                <p>${this.text(args["content"] ?? "")}</p>
                <ul>${Object.keys(items).map(key => `<li><label class="check danger"><input type="checkbox" value="${key}"${checked.includes(key) ? " checked" : ""} /><span></span></label></li>`).join("")}</ul>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${this.langs["Cancel"]}</button>
                <button type="button" class="primary"><i class="fi fiCheckmark"></i> ${this.langs["Confirm"]}</button>
            </footer>
        `;
        diag.querySelector(":scope > header > span").textContent = this.text(args["title"] || "@Confirm");
        diag.querySelectorAll("ul input").forEach(p => p.nextElementSibling.textContent = this.text(CheckConfirm.itemText(items[p.value])));
        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        applyBtn.addEventListener("click", () => diag["closer"]([...diag.querySelectorAll("ul input:checked")].map(p => p.value)));
        return diag;
    }
    /**
     * 체크 옵션 정의에서 표시 문구를 꺼낸다.
     * @param item 문구 또는 { text, checked }
     */
    static itemText(item) {
        return typeof item === "string" ? item : item.text;
    }
    /**
     * "@"로 시작하면 다국어 리소스 값으로, 아니면 그대로 반환한다.
     * @param value 문구 또는 "@키"
     */
    text(value) {
        return value.startsWith("@") ? (this.langs[value.slice(1)] ?? value.slice(1)) : value;
    }
}
//# sourceMappingURL=checkConfirm.js.map