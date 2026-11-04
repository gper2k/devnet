/**
 * 역할/그룹/하위 포함 그룹/사용자 대상을 추가·삭제하는 공용 대상 편집 모달 (읽기 권한 등 대상 목록 편집용).
 * common.modal.append("targetEdit", args) 로 열며, 확인 시 대상 목록(ITargetItem[], 비어 있을 수 있음), 취소/Esc 시 null 로 닫힌다.
 */
export default class TargetEdit {
    static types = ["Role", "Group", "Groups", "User"];
    static icons = { Role: "fiShieldAlt", Group: "fiUsers", Groups: "fiUsersGroup", User: "fiUser" };
    langs;
    /**
     * TargetEdit 기본 생성자
     */
    constructor() {
        this.langs = {};
    }
    /**
     * 모달 문구 다국어 사전 비동기 로드
     */
    async loadAsync(args) {
        this.langs = await window["common"].getLangsAsync(`Cancel, Apply, ${TargetEdit.types.join(", ")}`);
    }
    /**
     * 대화상자 DOM 생성 및 이벤트 바인딩
     */
    render(args) {
        const common = window["common"];
        const l = this.langs;
        const diag = document.createElement("dialog");
        diag.className = "targetEdit";
        diag.innerHTML = `
            <header><i class="fi fiShieldKeyhole fiFill"></i> ${args["title"] ?? ""}</header>
            <section>
                <header>
                    <div class="tools">
                        <label>ADD</label>
                        ${TargetEdit.types.map(p => `<span data-type="${p}"><i class="fi ${TargetEdit.icons[p]}"></i> ${l[p]}</span>`).join("")}
                    </div>
                </header>
                <ul></ul>
            </section>
            <footer>
                <button type="button" data-mode="cancel"><i class="fi fiProhibit"></i> ${l["Cancel"]}</button>
                <button type="button" class="main" data-mode="apply"><i class="fi fiCheckmark"></i> ${l["Apply"]}</button>
            </footer>
        `;
        const list = diag.querySelector("section > ul");
        const add = (type, key, name) => {
            if ([...list.children].some(li => li.dataset.type === type && li.dataset.key === key))
                return;
            const li = document.createElement("li");
            li.dataset.type = type;
            li.dataset.key = key;
            li.innerHTML = `<i class="badge sm deco"></i><span></span><button type="button" class="danger"><i class="fi fiDismiss"></i></button>`;
            li.querySelector(".badge").textContent = l[type] ?? type;
            li.querySelector("span").textContent = name;
            list.appendChild(li);
        };
        (args["items"] ?? []).forEach(p => add(p.type, p.key, p.name));
        diag.querySelectorAll("section > header .tools [data-type]").forEach(btn => btn.onclick = async () => {
            const type = btn.dataset.type;
            const result = await common.modal.append((type === "Groups" ? "group" : type.toLowerCase()) + "Select", { max: 0 });
            if (result && typeof result === "object")
                Object.entries(result).forEach(([key, text]) => add(type, key, text));
        });
        list.onclick = e => e.target.closest("button")?.closest("li")?.remove();
        diag.querySelector("[data-mode=\"cancel\"]").onclick = () => diag["closer"](null);
        diag.querySelector("[data-mode=\"apply\"]").onclick = () => diag["closer"]([...list.children].map(li => ({ type: li.dataset.type, key: li.dataset.key, name: li.querySelector("span").textContent })));
        return diag;
    }
}
//# sourceMappingURL=targetEdit.js.map