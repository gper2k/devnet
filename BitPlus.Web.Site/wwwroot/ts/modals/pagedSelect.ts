import IModal from "../IModal.js";

/**
 * 검색 + 페이징 선택 모달 공통 인자 규격 (역할/사용자 선택 등)
 */
export interface IPagedSelectArgs {
    /** 대화상자 제목 (생략 시 하위 모달의 다국어 제목) */
    title?: string;
    /** 최대 선택 항목 수 (0 또는 생략 시 무제한, 1 이면 단일 선택처럼 동작) */
    max?: number;
    /** 선택 불가 키 목록 (예: 이미 추가된 항목) */
    disableKeys?: string[];
    /** 초기 선택 (Key: 키, Value: 표시명) — 페이지에 없는 항목도 결과에 유지된다 */
    selected?: Record<string, string>;
    /** 페이지 크기 (기본 10) */
    pageSize?: number;
}

/**
 * 검색 + 페이징 목록에서 체크상자로 항목을 선택하는 모달 공통 베이스 (roleSelect, userSelect 가 상속).
 * 하위 클래스는 조회 API 경로, 제목 다국어 키, 아이콘, 루트 클래스만 정한다.
 * 확인 시 { 키: 표시명 } 객체, 취소 시 null 로 닫히며, 검색어가 없으면 전체 목록, 페이지를 넘겨도 선택 상태는 유지된다.
 * 조회 API 응답: { items: [{ key, text, sub? }], total } (page 는 1부터)
 */
export default abstract class PagedSelect implements IModal {
    /** 목록 조회 API 경로 (keyword/page/pageSize 쿼리를 붙여 호출) */
    protected abstract readonly listUrl: string;
    /** 기본 제목 다국어 키 */
    protected abstract readonly titleKey: string;
    /** 제목 아이콘 클래스 */
    protected abstract readonly icon: string;
    /** 대화상자 루트 클래스 */
    protected abstract readonly rootClass: string;

    private langs: Record<string, string>;
    private selected: Map<string, string>;
    private keyword: string;

    /**
     * PagedSelect 기본 생성자
     */
    constructor() {
        this.langs = {};
        this.selected = new Map();
        this.keyword = "";
    }

    /**
     * 모달 문구 다국어 사전 비동기 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        this.selected = new Map(Object.entries(args["selected"] ?? {}));
        this.langs = await window["common"].getLangsAsync(`Cancel, Confirm, Search, MaxSelectMsg, ${this.titleKey}`);
    }

    /**
     * 대화상자 DOM 생성 (검색바 + 목록 + 페이저 + 취소/확인)
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const diag = document.createElement("dialog");
        diag.className = this.rootClass;
        diag.innerHTML = `
            <header><i class="fi ${this.icon} fiFill"></i> ${args["title"] || this.langs[this.titleKey]}</header>
            <div>
                <div class="inputGroup">
                    <input type="search" placeholder="${this.langs["Search"]}" />
                    <button type="button" class="info"><i class="fi fiSearch"></i></button>
                </div>
                <ul class="selectList"></ul>
                <ul class="pager"></ul>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${this.langs["Cancel"]}</button>
                <button type="button" class="main"><i class="fi fiCheckmark"></i> ${this.langs["Confirm"]}</button>
            </footer>
        `;

        const searchInput = diag.querySelector<HTMLInputElement>("input[type=\"search\"]")!;
        const list = diag.querySelector<HTMLElement>("ul.selectList")!;
        const load = (page: number) => this.loadPage(list, page, args);
        const search = () => {
            this.keyword = searchInput.value.trim();
            load(0);
        };

        diag.querySelector(".inputGroup > button")!.addEventListener("click", search);
        searchInput.addEventListener("keydown", e => { if (e.key === "Enter") search(); });
        list.addEventListener("change", e => this.toggle(e.target as HTMLInputElement, list, args["max"] ?? 0));

        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        applyBtn.addEventListener("click", () => diag["closer"](Object.fromEntries(this.selected)));

        load(0);
        return diag;
    }

    /**
     * 목록 한 페이지를 조회해 목록과 페이저(common.makePager)를 다시 그린다 (선택/비활성 상태 반영).
     * @param list 목록 ul
     * @param page 페이지 번호 (0부터)
     * @param args 모달 인자
     */
    private async loadPage(list: HTMLElement, page: number, args: Record<string, any>) {
        const common = window["common"];
        const pageSize: number = args["pageSize"] ?? 10;
        const disableKeys: string[] = args["disableKeys"] ?? [];
        const query = `keyword=${encodeURIComponent(this.keyword)}&page=${page + 1}&pageSize=${pageSize}`;
        const res = await common.getSend(`${this.listUrl}?${query}`, { useLoading: false });
        const data = res?.data ?? { items: [], total: 0 };

        list.innerHTML = "";
        (data.items as { key: string; text: string; sub?: string }[]).forEach(p => {
            const li = document.createElement("li");
            li.innerHTML = `<label class="check"><input type="checkbox" value="${p.key}" /><span></span></label>`;
            const input = li.querySelector("input")!;
            input.checked = this.selected.has(p.key);
            input.disabled = disableKeys.includes(p.key);
            input.dataset.text = p.text;
            li.querySelector("span")!.textContent = p.text;
            if (p.sub) {
                const small = document.createElement("small");
                small.textContent = p.sub;
                li.querySelector("label")!.appendChild(small);
            }
            list.appendChild(li);
        });
        common.syncEmpty(list);
        list.parentElement!.querySelector(":scope > ul.pager")!
            .replaceWith(common.makePager(page, pageSize, data.total).then(p => this.loadPage(list, p, args)));
    }

    /**
     * 체크 변경을 선택 상태에 반영한다. 최대 선택 수가 1 이면 기존 선택을 대체하고, 그 외 초과 시 체크를 되돌리고 안내한다.
     * @param input 변경된 체크상자
     * @param list 목록 ul
     * @param max 최대 선택 수 (0 이면 무제한)
     */
    private toggle(input: HTMLInputElement, list: HTMLElement, max: number) {
        if (!input.checked) {
            this.selected.delete(input.value);
            return;
        }
        if (max === 1) {
            this.selected.clear();
            list.querySelectorAll<HTMLInputElement>("input[type=\"checkbox\"]:checked").forEach(p => { if (p !== input) p.checked = false; });
        } else if (max > 0 && this.selected.size >= max) {
            input.checked = false;
            window["common"].alert((this.langs["MaxSelectMsg"] ?? "").replace("{0}", String(max)));
            return;
        }
        this.selected.set(input.value, input.dataset.text ?? "");
    }
}
