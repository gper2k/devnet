import IModal from "../IModal.js";

/**
 * 메뉴얼 페이지 순서 변경 모달 인자 규격
 */
export interface IManualOrderArgs {
    /** 게시판 규칙 번호 */
    ruleNo: number;
    /** 순서를 바꿀 페이지 그룹의 글번호 */
    groupNo: number;
    /** 그 그룹이 속한 카테고리 값 (없으면 빈 문자열) */
    category?: string;
}

/**
 * 메뉴얼 페이지 순서 변경 모달: common.modal.append("manualOrder", args) 로 열며, 그룹의 하위 페이지를 끌어서 재정렬한 뒤 저장하면 true, 닫으면 null 로 닫힌다.
 * 페이지 목록은 목차와 같은 API(api/content/list)에서 받아 그 그룹 아래 페이지(레벨 1)만 보여 주고, 저장은 api/content/manual/order 로 한다 (관리자와 그룹 작성자만 서버가 허용).
 */
export default class ManualOrder implements IModal {
    private langs: Record<string, string>;

    /**
     * ManualOrder 기본 생성자
     */
    constructor() {
        this.langs = {};
    }

    /**
     * 문구 다국어 사전 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        this.langs = await window["common"].getLangsAsync("Cancel, Save, ManualOrderTitle, ManualOrderHint, DataEmpty");
    }

    /**
     * 대화상자 DOM 생성 (제목 + 안내 + 끌어서 정렬하는 페이지 목록 + 취소/저장)
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const common = window["common"];
        const t = this.langs;
        const a = args as IManualOrderArgs;
        const diag = document.createElement("dialog");
        diag.className = "manualOrder";
        diag.innerHTML = `
            <header><i class="fi fiSort"></i> <span>${t["ManualOrderTitle"]}</span></header>
            <div>
                <p>${t["ManualOrderHint"]}</p>
                <ul></ul>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${t["Cancel"]}</button>
                <button type="button" class="primary"><i class="fi fiSave"></i> ${t["Save"]}</button>
            </footer>
        `;
        const list = diag.querySelector("ul")!;
        const [cancelBtn, saveBtn] = diag.querySelectorAll<HTMLButtonElement>("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));

        /** 화면에 놓인 순서대로의 페이지 글번호 */
        const orderNow = () => [...list.children].map(li => Number((li as HTMLElement).dataset["no"]));
        saveBtn.addEventListener("click", async () => {
            const res = await common.postSend("/api/content/manual/order", common.makeFormData({ ruleNo: a.ruleNo, groupNo: a.groupNo, order: JSON.stringify(orderNow()) }));
            if (res?.success) diag["closer"](true);
        });

        void (async () => {
            const res = await common.getSend(`/api/content/list?${new URLSearchParams({ ruleNo: String(a.ruleNo), category: a.category ?? "", size: "100000" })}`, { useLoading: false });
            if (!res?.success) return;
            // 목록은 그룹 → 그 아래 페이지 순서: 이 그룹을 찾아 다음 그룹이 나오기 전까지의 페이지가 이 그룹의 페이지
            const items: { contentNo: number; title: string; level: number }[] = res.data.items;
            const start = items.findIndex(p => p.contentNo === a.groupNo && p.level === 0);
            const pages: typeof items = [];
            for (let i = start + 1; start >= 0 && i < items.length && items[i].level > 0; i++) pages.push(items[i]);
            common.bindList(list, pages, (p: { contentNo: number; title: string }) => common.html`<li draggable="true" data-no="${p.contentNo}"><i class="fi fiReOrderDotsVert"></i><span>${p.title}</span></li>`, common.html`<li class="empty">${t["DataEmpty"]}</li>`);
            saveBtn.disabled = pages.length < 2;
            common.bindDragSort(list);
        })();
        return diag;
    }
}
