/**
 * 문화권별 값을 입력받는 공용 다국어 입력 모달 (표시명 등 다국어 값 편집용).
 * 지원 문화권(코드 → 네이티브명)과 모달 문구는 /api/system/langinput 한 번의 호출로 받아오며, 현재 문화권을 맨 위에 표시한다.
 * 적용 시 빈 값을 제외한 { 문화권: 값 } JSON 문자열로, 취소 시 빈 문자열로 닫힌다.
 */
export default class LangInput {
    cultures;
    langs;
    title;
    /**
     * LangInput 기본 생성자
     */
    constructor() {
        this.cultures = {};
        this.langs = {};
        this.title = "";
    }
    /**
     * 지원 문화권 및 모달 문구(제목/버튼) 다국어 값 비동기 로드
     */
    async loadAsync(args) {
        const title = args["title"] || "@LangInput";
        const titleKey = title.startsWith("@") ? title.slice(1) : null;
        const keys = ["Cancel", "Confirm", ...(titleKey ? [titleKey] : [])].join(",");
        const res = await window["common"].getSend(`/api/system/langinput?keys=${encodeURIComponent(keys)}`, { useLoading: false });
        this.cultures = res?.data?.cultures ?? {};
        this.langs = res?.data?.langs ?? {};
        this.title = titleKey ? (this.langs[titleKey] || titleKey) : title;
    }
    /**
     * 대화상자 DOM 생성 (문화권별 입력항 + 취소/적용)
     */
    render(args) {
        const cultures = this.cultures;
        const values = args["values"] ?? {};
        const current = window["common"].culture;
        const codes = Object.keys(cultures).sort((a, b) => Number(b === current) - Number(a === current));
        const diag = document.createElement("dialog");
        diag.className = "langInput";
        diag.innerHTML = `
            <header><i class="fi fiTranslate fiFill"></i> ${this.title}</header>
            <ul class="form">
                ${codes.map(code => `<li><header>${cultures[code]} (${code})</header><input type="text" data-culture="${code}" /></li>`).join("")}
            </ul>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${this.langs["Cancel"] || "Cancel"}</button>
                <button type="button" class="main"><i class="fi fiCheckmark"></i> ${this.langs["Confirm"] || "Confirm"}</button>
            </footer>
        `;
        const inputs = [...diag.querySelectorAll("input[data-culture]")];
        inputs.forEach(el => el.value = values[el.dataset.culture] ?? "");
        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"]());
        applyBtn.addEventListener("click", () => {
            const map = {};
            inputs.forEach(el => { if (el.value.trim() !== "")
                map[el.dataset.culture] = el.value.trim(); });
            diag["closer"](JSON.stringify(map));
        });
        return diag;
    }
}
//# sourceMappingURL=langInput.js.map