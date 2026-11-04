/**
 * 메뉴를 등록/수정/삭제하는 공용 메뉴 편집 모달 (탭: 기본 정보 / 접근 권한). 저장·삭제 API 호출까지 모달이 처리한다.
 * common.modal.append("menuEdit", args) 로 열며, 저장하면 { key } (저장된 메뉴 키), 삭제하면 { key, deleted: true }, 취소/Esc 이면 null 로 닫힌다.
 * 시스템 메뉴는 이동 형식·확장값·경로·사용 여부·접근 권한 변경과 삭제가 막힌다 (서버도 무시/거부).
 */
export default class MenuEdit {
    static linkTypes = ["None", "Move", "Open", "Script", "Mail", "Phone", "Extend"];
    /** 게시판 메뉴들의 최상위 시스템 메뉴 키: 이 메뉴 이하의 메뉴는 상위 메뉴를 이 메뉴 이하에서만 고를 수 있다 */
    static contentMapKey = "ContentMap";
    static accessTypes = ["Role", "Group", "Groups", "User"];
    static accessIcons = { Role: "fiShieldAlt", Group: "fiUsers", Groups: "fiUsersGroup", User: "fiUser" };
    langs;
    nodes;
    menu;
    /**
     * MenuEdit 기본 생성자
     */
    constructor() {
        this.langs = {};
        this.nodes = [];
        this.menu = null;
    }
    /**
     * 모달 문구 다국어 사전, 메뉴 트리(상위 메뉴 선택용), 수정 대상 메뉴 비동기 로드
     */
    async loadAsync(args) {
        const common = window["common"];
        const keys = ["Cancel", "Save", "Delete", "BasicInfo", "FileAccessRule", "Menu", "KeyOf", "ParentMenu", "TopLevel", "DpName", "LangInput", "Icon", "LinkType",
            "ExtendValue", "MenuAction", "Enabler", "Hide", "MenuKeyRule", "AddOf", "EditOf", "DelMsg", "DelWithChildMenu", "DelWarning",
            ...MenuEdit.accessTypes, ...MenuEdit.linkTypes.map(p => "LinkType" + p)];
        this.langs = await common.getLangsAsync(keys.join(", "));
        this.nodes = (await common.getSend("/api/sitemap/menu/tree", { useLoading: false }))?.data ?? [];
        this.menu = null;
        if (args["editKey"]) {
            const res = await common.getSend(`/api/sitemap/menu/item/${encodeURIComponent(args["editKey"])}`);
            if (!res?.success)
                throw new Error("menu not found");
            this.menu = res.data;
        }
    }
    /**
     * 대화상자 DOM 생성 및 이벤트 바인딩
     */
    render(args) {
        const common = window["common"];
        const l = this.langs;
        const menu = this.menu;
        const init = (menu ? {} : args["values"] ?? {});
        const title = (menu ? l["EditOf"] : l["AddOf"]).replace("{0}", l["Menu"]);
        const diag = document.createElement("dialog");
        diag.className = "menuEdit";
        diag.innerHTML = `
            <header><i class="fi fiMenu"></i> <span>${title}</span> <span class="badge" hidden>SYS</span></header>
            <section class="tabBox">
                <header>
                    <span data-for=".tabBasic" class="active">${l["BasicInfo"]}</span>
                    <span data-for=".tabAccess">${l["FileAccessRule"]}</span>
                </header>
                <section>
                    <div class="tabBasic active">
                        <ul class="form">
                            <li class="col2">
                                <div>
                                    <header>${l["KeyOf"].replace("{0}", l["Menu"])}</header>
                                    <input type="text" name="key" maxlength="64" placeholder="${l["MenuKeyRule"]}" autocomplete="off" />
                                </div>
                                <div>
                                    <header>${l["ParentMenu"]}</header>
                                    <select name="parentKey"></select>
                                </div>
                            </li>
                            <li class="col2">
                                <div>
                                    <header>${l["DpName"]}</header>
                                    <div class="inputGroup">
                                        <input type="text" name="dpNames" data-lang="{}" data-title="${l["DpName"]}" />
                                        <button type="button" title="${l["LangInput"]}"><i class="fi fiTranslate"></i></button>
                                    </div>
                                </div>
                                <div>
                                    <header>${l["Icon"]}</header>
                                    <input type="text" name="icon" placeholder="&lt;i class=&quot;fi fiHome&quot;&gt;&lt;/i&gt;" autocomplete="off" />
                                </div>
                            </li>
                            <li class="col2">
                                <div>
                                    <header>${l["LinkType"]}</header>
                                    <select name="type">${MenuEdit.linkTypes.map(p => `<option value="${p}">${l["LinkType" + p]}</option>`).join("")}</select>
                                </div>
                                <div>
                                    <header>${l["ExtendValue"]}</header>
                                    <input type="text" name="extend" maxlength="32" placeholder="app" autocomplete="off" />
                                </div>
                            </li>
                            <li>
                                <header>${l["MenuAction"]}</header>
                                <input type="text" name="action" maxlength="1024" placeholder="/Home" autocomplete="off" />
                            </li>
                            <li class="right">
                                <label class="check main"><input type="checkbox" name="enabler" checked /><span>${l["Enabler"]}</span></label>
                                <label class="check"><input type="checkbox" name="hidden" /><span>${l["Hide"]}</span></label>
                            </li>
                        </ul>
                    </div>
                    <div class="tabAccess">
                        <header>
                            <div class="tools">
                                <label>ADD</label>
                                ${MenuEdit.accessTypes.map(p => `<span data-type="${p}"><i class="fi ${MenuEdit.accessIcons[p]}"></i> ${l[p]}</span>`).join("")}
                            </div>
                        </header>
                        <ul class="accessItems"></ul>
                    </div>
                </section>
            </section>
            <footer>
                <button type="button" class="danger" hidden><i class="fi fiDelete"></i> ${l["Delete"]}</button>
                <button type="button" data-mode="cancel"><i class="fi fiBan"></i> ${l["Cancel"]}</button>
                <button type="button" class="main" data-mode="save"><i class="fi fiSave"></i> ${l["Save"]}</button>
            </footer>
        `;
        const field = (name) => diag.querySelector(`[name="${name}"]`);
        const accessList = diag.querySelector("ul.accessItems");
        const firstTab = diag.querySelector(".tabBox > header > :first-child");
        const isSystem = !!menu?.["isSystem"];
        // 상위 메뉴 목록 (수정 중이면 자기 자신과 하위는 제외)
        // 컨텐츠맵 이하 메뉴(수정 중이거나 새 메뉴의 상위가 컨텐츠맵 이하)는 컨텐츠맵과 그 하위 메뉴 중에서만 고른다 (최상위 선택 없음)
        const parent = field("parentKey");
        const mapNode = MenuEdit.find(this.nodes, MenuEdit.contentMapKey);
        const inMap = !!mapNode && (menu ? menu["key"] !== MenuEdit.contentMapKey && !!MenuEdit.find(mapNode.child ?? [], menu["key"]) : !!args["parentKey"] && !!MenuEdit.find([mapNode], args["parentKey"]));
        if (!inMap)
            parent.add(new Option(l["TopLevel"], ""));
        MenuEdit.flatten(inMap ? [mapNode] : this.nodes, menu?.["key"] ?? null).forEach(([key, text]) => parent.add(new Option(text, key)));
        parent.value = (menu ? menu["parentKey"] : args["parentKey"]) ?? "";
        field("key").value = menu?.["key"] ?? init.key ?? "";
        field("key").readOnly = !!menu;
        common.setLang(field("dpNames"), menu?.["dpNames"] ?? init.dpNames ?? {});
        field("type").value = menu?.["type"] ?? init.type ?? "Move";
        field("icon").value = menu?.["icon"] ?? init.icon ?? "";
        field("extend").value = menu?.["extend"] ?? init.extend ?? "";
        field("action").value = menu?.["action"] ?? init.action ?? "";
        field("enabler").checked = menu?.["enabler"] ?? init.enabler ?? true;
        field("hidden").checked = !(menu?.["visible"] ?? init.visible ?? true); // 감춤: 시스템 메뉴도 바꿀 수 있다(아래 잠금 대상이 아님)
        (menu?.["accesses"] ?? init.accesses ?? []).forEach(a => this.appendAccess(accessList, diag, a.type, a.key, a.name));
        // 형식이 확장일 때만 확장값 입력 가능, 시스템 메뉴는 형식·확장값·경로·사용 여부·접근 권한 변경 불가 (감춤은 가능)
        const syncExtend = () => field("extend").disabled = isSystem || field("type").value !== "Extend";
        ["type", "action", "enabler"].forEach(name => field(name).disabled = isSystem);
        diag.querySelector(".tabAccess").inert = isSystem;
        diag.querySelector(":scope > header > .badge").hidden = !isSystem;
        syncExtend();
        field("type").onchange = syncExtend;
        // 접근 권한: 형식별 선택 모달(여러 개 선택)로 추가, 행의 X 로 제거
        diag.querySelectorAll(".tabAccess .tools [data-type]").forEach(btn => btn.onclick = async () => {
            const type = btn.dataset.type;
            const result = await common.modal.append((type === "Groups" ? "group" : type.toLowerCase()) + "Select", { max: 0 });
            if (result && typeof result === "object")
                Object.entries(result).forEach(([key, text]) => this.appendAccess(accessList, diag, type, key, text));
        });
        accessList.onclick = e => e.target.closest("button")?.closest("li")?.remove();
        const deleteBtn = diag.querySelector("footer > button.danger");
        deleteBtn.hidden = !menu || isSystem; // 시스템 메뉴는 삭제 불가
        deleteBtn.onclick = async () => {
            if (!await common.confirm(`${l["DelMsg"].replace("{0}", l["Menu"])} ${l["DelWithChildMenu"]} ${l["DelWarning"]}`))
                return;
            const res = await common.delSend(`/api/sitemap/menu/item/${encodeURIComponent(menu["key"])}`);
            if (res?.success)
                diag["closer"]({ key: menu["key"], deleted: true });
        };
        diag.querySelector("footer > [data-mode=\"cancel\"]").onclick = () => diag["closer"](null);
        diag.querySelector("footer > [data-mode=\"save\"]").onclick = async () => {
            const key = field("key").value.trim();
            if (!/^[A-Za-z0-9_.-]{1,64}$/.test(key)) {
                firstTab.click();
                field("key").focus();
                return common.alert(l["MenuKeyRule"]);
            }
            const body = {
                key,
                parentKey: parent.value,
                dpNames: common.getLang(field("dpNames")),
                type: field("type").value,
                icon: field("icon").value.trim(),
                extend: field("extend").value.trim(),
                action: field("action").value.trim(),
                enabler: field("enabler").checked,
                visible: !field("hidden").checked,
                accesses: JSON.stringify([...accessList.children].map(li => ({ type: li.dataset.type, key: li.dataset.key })))
            };
            const res = menu
                ? await common.putSend(`/api/sitemap/menu/item/${encodeURIComponent(menu["key"])}`, common.makeFormData(body))
                : await common.postSend("/api/sitemap/menu", common.makeFormData(body));
            if (res?.success)
                diag["closer"]({ key });
        };
        return diag;
    }
    /**
     * 접근 권한 대상 행을 추가한다 (같은 형식+키는 한 번만).
     * @param list 접근 권한 목록
     * @param diag 대화상자 (형식 표시명을 추가 버튼 문구에서 가져옴)
     * @param type 대상 형식 (Role/Group/Groups/User)
     * @param key 대상 키
     * @param name 대상 표시명
     */
    appendAccess(list, diag, type, key, name) {
        if ([...list.children].some(li => li.dataset.type === type && li.dataset.key === key))
            return;
        const li = document.createElement("li");
        li.dataset.type = type;
        li.dataset.key = key;
        li.innerHTML = `<i class="badge sm deco"></i><span></span><button type="button" class="danger"><i class="fi fiDismiss"></i></button>`;
        li.querySelector(".badge").textContent = diag.querySelector(`.tabAccess .tools [data-type="${type}"]`)?.textContent?.trim() ?? type;
        li.querySelector("span").textContent = name;
        li.querySelector("button").disabled = !!this.menu?.["isSystem"];
        list.appendChild(li);
    }
    /**
     * 메뉴 트리에서 키로 노드를 찾는다 (하위 포함).
     * @param nodes 트리 노드 목록
     * @param key 찾을 메뉴 키
     */
    static find(nodes, key) {
        for (const n of nodes) {
            const found = n.key === key ? n : MenuEdit.find(n.child ?? [], key);
            if (found)
                return found;
        }
        return null;
    }
    /**
     * 메뉴 트리를 [키, 깊이만큼 들여쓴 표시명] 목록으로 펼친다. skipKey 와 그 하위는 뺀다 (자기/하위를 상위로 고를 수 없음).
     * @param nodes 트리 노드 목록
     * @param skipKey 제외할 메뉴 키
     * @param depth 현재 깊이
     */
    static flatten(nodes, skipKey, depth = 0) {
        return nodes.flatMap(n => n.key === skipKey ? [] : [[n.key, `${"　".repeat(depth)}${n.text}`], ...MenuEdit.flatten(n.child ?? [], skipKey, depth + 1)]);
    }
}
//# sourceMappingURL=menuEdit.js.map