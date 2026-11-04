/* ==========================================================================
   TreeBox: 윈도우 탐색기 폴더 트리 컨트롤
   ========================================================================== */
import { showPopMenu } from "./PopMenu.js";
/** 트리 주 색상으로 쓸 수 있는 사이트 색상 이름 */
const TREE_COLORS = ["main", "sub", "point", "deco", "primary", "info", "success", "warning", "danger"];
/** 확장 메뉴 아이콘 제목 (다국어 "옵션", 처음 쓸 때 한 번만 조회) */
let moreTitle = null;
/** 트리 컨트롤 클래스 */
export default class TreeBox {
    wrap;
    option;
    nodeMap;
    data;
    constructor(option) {
        this.wrap = null;
        this.nodeMap = new Map();
        this.data = [];
        this.option = {
            openSaveKey: null,
            onSelect: () => { },
            useAllOpen: false,
            useCheck: false,
            useOpenEvent: true,
            selectedKeys: [],
            openKeys: null,
            menus: undefined,
            noMenuKeys: [],
            ...option
        };
    }
    getOpens = () => [...(this.wrap?.querySelectorAll(":scope li.opened") ?? [])].map(p => p.getAttribute("data-key"));
    getOpenKeys = () => ((this.option.openSaveKey ? sessionStorage.getItem(this.option.openSaveKey) : "") ?? "").split(",").filter(p => p);
    setOpenKeys = (keys) => {
        if (this.option.openSaveKey)
            sessionStorage.setItem(this.option.openSaveKey, keys.join(","));
    };
    onOpen = () => this.setOpenKeys(this.getOpens());
    /**
     * 트리의 주 색상을 바꾼다 (이름은 <see cref="ITreeOption.color"/> 참고, 모르는 이름이나 빈 값이면 기본으로 되돌림). 이미 그려진 트리에도 바로 적용된다.
     * @param color 사이트 색상 이름
     */
    setColor(color) {
        this.option.color = color ?? undefined;
        this.applyColor();
    }
    /** 옵션의 주 색상을 트리 영역의 --main 변수로 덮어쓴다 (트리 안의 아이콘·선택 막대가 모두 --main 을 쓴다). */
    applyColor() {
        const name = this.option.color;
        if (name && TREE_COLORS.includes(name) && name !== "main")
            this.wrap?.style.setProperty("--main", `var(--${name})`);
        else
            this.wrap?.style.removeProperty("--main");
    }
    /** 선택된 항목 키 반환 */
    getSelect() { return this.wrap?.querySelector(":scope li.selected")?.getAttribute("data-key") || null; }
    /** 선택된 노드 데이터 반환 */
    getSelectNode() {
        const key = this.getSelect();
        if (!key)
            return null;
        return this.nodeMap.get(key)?.node ?? null;
    }
    /** 키에 해당하는 노드 데이터 반환 */
    getNode(key) { return this.nodeMap.get(key)?.node ?? null; }
    /** 체크된 항목 키 목록 반환 */
    getChecked() { return [...(this.wrap?.querySelectorAll(`:scope input[type="checkbox"]:checked`) ?? [])].map(p => p.value); }
    /** 체크된 항목의 노드 데이터 목록 반환 */
    getCheckedNodes() { return this.getChecked().map(p => this.nodeMap.get(p)?.node).filter((p) => !!p); }
    /**
     * 키 목록에 해당하는 체크상자 체크 (비활성 항목 제외)
     * @param keys 체크할 항목 키 목록
     */
    setChecked(keys) {
        keys.forEach(key => {
            const input = this.nodeMap.get(key)?.el.querySelector(":scope > div input[type=\"checkbox\"]");
            if (input && !input.disabled)
                input.checked = true;
        });
    }
    /** 수동 열기/닫기 처리 */
    setOpen(key, checked) {
        let openKeys = this.getOpenKeys();
        const item = this.wrap?.querySelector(`li[data-key="${key}"]`);
        if (checked) {
            if (!openKeys.some(p => p == key))
                openKeys = [key, ...openKeys];
            if (item && !item.classList.contains("opened"))
                item.classList.add("opened");
        }
        else {
            if (openKeys.some(p => p == key))
                openKeys = openKeys.filter(p => p != key);
            if (item && item.classList.contains("opened"))
                item.classList.remove("opened");
        }
        this.setOpenKeys(openKeys);
    }
    /** 전체 열기 */
    openAll() {
        this.wrap?.querySelectorAll("li").forEach(li => {
            if (li.querySelector(":scope > ul")) {
                li.classList.add("opened");
                const icon = li.querySelector(":scope > div > i.fi");
                if (icon) {
                    icon.classList.remove("fiFolderAlt");
                    icon.classList.add("fiFolderOpened");
                }
            }
        });
        this.setOpenKeys(this.getOpens());
    }
    /** 전체 닫기 */
    closeAll() {
        this.wrap?.querySelectorAll("li.opened").forEach(li => {
            li.classList.remove("opened");
            const icon = li.querySelector(":scope > div > i.fi");
            if (icon) {
                icon.classList.remove("fiFolderOpened");
                icon.classList.add("fiFolderAlt");
            }
        });
        this.setOpenKeys([]);
    }
    /** 선택 해제 */
    clearSelect() {
        this.wrap?.querySelectorAll("li.selected").forEach(p => p.classList.remove("selected"));
    }
    /**
     * 지정 키 항목만 선택 표시한다 (onSelect 는 호출하지 않음).
     * @param key 선택할 항목 키
     */
    select(key) {
        this.clearSelect();
        this.wrap?.querySelector(`li[data-key="${CSS.escape(key)}"]`)?.classList.add("selected");
    }
    /**
     * 노드 아이콘 태그 HTML 을 접기/펴기 아이콘 요소(i.fi)에 적용한다: 아이콘이 i 태그면 그 클래스를 옮겨 쓰고, 그 밖의 태그(span, svg)는 요소 안에 넣는다.
     * 열림 여부와 상관없이 같은 아이콘을 그대로 보인다 (폴더 모양 전환 없음).
     * @param fold 접기/펴기 아이콘 요소
     * @param icon 아이콘 태그 HTML
     */
    static applyIcon(fold, icon) {
        const holder = document.createElement("div");
        holder.innerHTML = icon;
        const source = holder.firstElementChild;
        if (!source)
            return;
        if (source.tagName === "I")
            source.classList.forEach(c => fold.classList.add(c));
        else
            fold.appendChild(source);
        fold.classList.add("customIcon");
    }
    /** 트리 항목 엘리먼트 생성 */
    makeItem(itemData, depth = 0) {
        const openKeys = this.option.openKeys ?? this.getOpenKeys();
        const item = document.createElement("li");
        const div = document.createElement("div");
        const fold = document.createElement("i");
        const subDiv = document.createElement("div");
        const span = document.createElement("span");
        if (this.option.selectedKeys?.some(p => p == itemData.key))
            item.classList.add("selected");
        if (itemData.disabled)
            item.classList.add("disabled");
        item.setAttribute("data-key", itemData.key);
        // depth 기반 padding-left 주입 (full-width bar)
        div.style.paddingLeft = `calc(${depth * 1.4}em + 0.5em)`;
        fold.classList.add("fi");
        const hasChild = itemData.child && itemData.child.length > 0;
        if (hasChild && (this.option.useAllOpen === true || openKeys?.some(p => p == itemData.key))) {
            item.classList.add("opened");
            if (!itemData.icon)
                fold.classList.add("fiFolderOpened");
        }
        else if (!itemData.icon)
            fold.classList.add("fiFolderAlt");
        if (!hasChild && !itemData.icon)
            fold.classList.add("solid");
        if (itemData.icon)
            TreeBox.applyIcon(fold, itemData.icon);
        if (this.option.useOpenEvent !== true)
            fold.classList.add("nonEvent");
        if (itemData.name === undefined)
            span.innerText = itemData.text;
        else
            span.innerHTML = itemData.text;
        subDiv.title = itemData.name ?? itemData.text;
        if (this.option.useCheck === true) {
            const checkLabel = document.createElement("label");
            checkLabel.className = "check";
            checkLabel.innerHTML = `<input type="checkbox" value="${itemData.key}"${itemData.disabled ? " disabled" : ""} />`;
            checkLabel.appendChild(span);
            subDiv.appendChild(checkLabel);
        }
        else
            subDiv.appendChild(span);
        if (this.option.useCheck === false && (this.option.useOpenEvent === false || this.option.useAllOpen === false))
            div.appendChild(fold);
        div.appendChild(subDiv);
        // 확장 메뉴 아이콘 (span.moreBtn)
        if (this.option.menus && this.option.menus.length > 0 && !this.option.noMenuKeys?.includes(itemData.key)) {
            const moreBtn = document.createElement("span");
            moreBtn.className = "moreBtn";
            (moreTitle ??= window["common"].getLangsAsync("SurveyOptions").then((l) => l["SurveyOptions"])).then(t => moreBtn.title = t);
            moreBtn.innerHTML = `<i class="fi fiEllipsisV"></i>`;
            div.appendChild(moreBtn);
        }
        item.appendChild(div);
        this.nodeMap.set(itemData.key, { node: itemData, el: item });
        if (hasChild && itemData.child) {
            item.appendChild(this.makeGroup(itemData.child, depth + 1));
        }
        return item;
    }
    /** 목록 엘리먼트 생성 */
    makeGroup(listData, depth = 0) {
        const group = document.createElement("ul");
        listData.forEach(p => group.appendChild(this.makeItem(p, depth)));
        return group;
    }
    /** 대상 영역에 렌더링 */
    render(target, bindData) {
        const wrap = target instanceof HTMLElement ? target : document.querySelector(target);
        if (!wrap)
            return;
        // 같은 영역에 다시 바인딩하면 현재 열림/선택 상태를 그대로 유지한다
        const keep = { openKeys: this.option.openKeys, selectedKeys: this.option.selectedKeys };
        if (wrap === this.wrap) {
            const selected = this.getSelect();
            this.option.openKeys = this.getOpens();
            this.option.selectedKeys = selected ? [selected] : [];
        }
        this.wrap = wrap;
        this.data = bindData;
        this.wrap.removeEventListener("click", this.handleClick);
        this.wrap.addEventListener("click", this.handleClick);
        this.wrap.innerHTML = "";
        this.applyColor();
        this.nodeMap.clear();
        this.wrap.appendChild(this.makeGroup(bindData, 0));
        Object.assign(this.option, keep);
    }
    /**
     * 노드 추가/수정/이동을 한 번에 처리한다. 같은 키가 있으면 하위 노드를 유지한 채 떼어낸 뒤,
     * parentKey 아래(없거나 찾지 못하면 최상위)의 정렬 위치(sort → text)에 다시 넣고 상태를 유지해 재렌더링한다.
     * @param node 반영할 노드 (key/text/sort 등)
     * @param parentKey 상위 노드 키
     */
    upsert(node, parentKey) {
        const old = this.detach(node.key);
        const parent = parentKey ? this.nodeMap.get(parentKey)?.node : null;
        const siblings = parent ? (parent.child ??= []) : this.data;
        siblings.push({ ...old, ...node, child: old?.child ?? node.child });
        siblings.sort((a, b) => TreeBox.compare(a, b));
        if (parentKey && this.wrap)
            this.wrap.querySelector(`li[data-key="${parentKey}"]`)?.classList.add("opened");
        this.refresh();
    }
    /**
     * 노드를 (하위 노드 포함) 제거하고 재렌더링한다.
     * @param key 제거할 노드 키
     */
    remove(key) {
        this.detach(key);
        this.refresh();
    }
    /** 현재 데이터를 열림/선택 상태를 유지한 채 다시 그린다. */
    refresh() {
        if (this.wrap)
            this.render(this.wrap, this.data);
    }
    /** 키에 해당하는 노드를 소속 목록에서 떼어내 반환한다. */
    detach(key, list = this.data) {
        const index = list.findIndex(p => p.key == key);
        if (index >= 0)
            return list.splice(index, 1)[0];
        for (const item of list) {
            const found = item.child ? this.detach(key, item.child) : null;
            if (found)
                return found;
        }
        return null;
    }
    /** 형제 정렬 비교: sort(대소문자 무시) → text 순 */
    static compare(a, b) {
        const sa = (a.sort ?? "").toUpperCase(), sb = (b.sort ?? "").toUpperCase();
        return sa < sb ? -1 : sa > sb ? 1 : (a.name ?? a.text).localeCompare(b.name ?? b.text);
    }
    handleClick = (e) => this.onClick(e);
    /** 항목별 클릭 처리기 */
    onClick(e) {
        const target = e.target;
        // 확장 메뉴 (...) 아이콘 클릭
        const moreBtnEl = target.closest("span.moreBtn");
        if (moreBtnEl) {
            e.stopPropagation();
            const li = moreBtnEl.closest("li");
            const key = li?.getAttribute("data-key");
            if (key) {
                const entry = this.nodeMap.get(key);
                if (entry)
                    this.showMenu(entry.node, moreBtnEl);
            }
            return;
        }
        // 폴더 아이콘 클릭 (열기/닫기)
        if (this.option.useOpenEvent === true) {
            const foldIcon = target.closest("li > div > i.fi");
            if (foldIcon && !foldIcon.classList.contains("nonEvent")) {
                const item = foldIcon.parentElement?.parentElement;
                if (item?.querySelector(":scope > ul")) {
                    const key = item.getAttribute("data-key");
                    if (item.classList.contains("opened")) {
                        item.classList.remove("opened");
                        [...item.querySelectorAll(":scope li.opened")].forEach(p => p.classList.remove("opened"));
                        foldIcon.classList.replace("fiFolderOpened", "fiFolderAlt");
                        this.wrap?.dispatchEvent(new CustomEvent("closed", { detail: { target: key } }));
                        this.onOpen();
                    }
                    else {
                        item.classList.add("opened");
                        foldIcon.classList.replace("fiFolderAlt", "fiFolderOpened");
                        this.wrap?.dispatchEvent(new CustomEvent("opened", { detail: { target: key } }));
                        this.onOpen();
                    }
                }
                return;
            }
        }
        // 노드 행 클릭 (선택 토글) — 텍스트뿐 아니라 행 전체를 클릭 영역으로 쓰되, 폴더 아이콘 클릭은 제외
        const rowEl = target.closest("li > div");
        if (rowEl && !target.closest("li > div > i.fi")) {
            const item = rowEl.parentElement;
            if (!item || item.classList.contains("disabled"))
                return;
            const key = item.getAttribute("data-key");
            const entry = this.nodeMap.get(key);
            if (item.classList.contains("selected")) {
                this.wrap?.querySelectorAll("li.selected").forEach(p => p.classList.remove("selected"));
                this.wrap?.dispatchEvent(new CustomEvent("unselected", { detail: { target: key } }));
                this.option.onSelect?.("unselect", key, entry?.node);
            }
            else {
                this.wrap?.querySelectorAll("li.selected").forEach(p => p.classList.remove("selected"));
                item.classList.add("selected");
                this.wrap?.dispatchEvent(new CustomEvent("selected", { detail: { target: key } }));
                this.option.onSelect?.("select", key, entry?.node);
            }
        }
    }
    /** 확장 메뉴 표시 (노드별 숨김 조건 적용) */
    showMenu(node, anchorEl) {
        const entry = this.nodeMap.get(node.key);
        showPopMenu(anchorEl, (this.option.menus ?? []).filter(m => !m.hidden?.(node))
            .map(m => ({ ...m, action: () => m.action?.(node, entry?.el, this) })));
    }
}
//# sourceMappingURL=TreeBox.js.map