import IModal from "../IModal.js";
import TreeBox, { ITreeNode } from "../TreeBox.js";

/**
 * 그룹 선택 모달 인자 규격
 */
export interface IGroupSelectArgs {
    /** 대화상자 제목 (생략 시 다국어 "GroupSelect") */
    title?: string;
    /** 최대 선택 항목 수 (0 또는 생략 시 무제한, 1 이면 단일 선택처럼 동작) */
    max?: number;
    /** 숨김(Visible=false) 그룹 포함 여부 (기본 false) */
    includeHidden?: boolean;
    /** 초기 체크 그룹 키 목록 */
    selectedKeys?: string[];
    /** 선택 불가 그룹 키 목록 (예: 이미 추가된 그룹) */
    disableKeys?: string[];
    /** disableKeys 의 하위 그룹까지 선택 불가 처리 여부 (기본 false, 상위 그룹 지정 시 순환 방지용으로 true) */
    disableChildren?: boolean;
}

/**
 * 체크상자로 그룹을 선택하는 공용 그룹 선택 모달.
 * common.modal.append("groupSelect", args) 로 열며, 확인 시 { 그룹키: 표시명 } 객체, 취소/Esc 시 null 또는 빈 문자열로 닫힌다.
 * 아무것도 체크하지 않고 확인하면 빈 객체({})를 반환한다.
 */
export default class GroupSelect implements IModal {
    private langs: Record<string, string>;
    private treeData: ITreeNode[];
    private tree: TreeBox;

    /**
     * GroupSelect 기본 생성자 (전체 펼침 + 체크상자 트리 구성)
     */
    constructor() {
        this.langs = {};
        this.treeData = [];
        this.tree = window["common"].makeTree({ useAllOpen: true, useCheck: true, useOpenEvent: false });
    }

    /**
     * 모달 문구 다국어 사전 및 그룹 트리 데이터 비동기 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        const common = window["common"];
        this.langs = await common.getLangsAsync("Cancel, Confirm, GroupSelect, MaxSelectMsg");
        const res = await common.getSend(`/api/membership/group/tree?includeHidden=${args["includeHidden"] === true}`);
        this.treeData = res?.data ?? [];
        GroupSelect.disable(this.treeData, args["disableKeys"] ?? [], args["disableChildren"] === true, false);
    }

    /**
     * 대화상자 DOM 생성 (그룹 트리 + 취소/확인)
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const max: number = args["max"] ?? 0;
        const diag = document.createElement("dialog");
        diag.className = "groupSelect";
        diag.innerHTML = `
            <header><i class="fi fiPeopleCheck fiFill"></i> ${args["title"] || this.langs["GroupSelect"]}</header>
            <div class="treeBox"></div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${this.langs["Cancel"]}</button>
                <button type="button" class="main"><i class="fi fiCheckmark"></i> ${this.langs["Confirm"]}</button>
            </footer>
        `;

        const treeEl = diag.querySelector<HTMLElement>(":scope > div")!;
        this.tree.render(treeEl, this.treeData);
        this.tree.setChecked(args["selectedKeys"] ?? []);
        treeEl.addEventListener("change", e => this.limit(e.target as HTMLInputElement, treeEl, max));

        const [cancelBtn, applyBtn] = diag.querySelectorAll("footer > button");
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        applyBtn.addEventListener("click", () =>
            diag["closer"](Object.fromEntries(this.tree.getCheckedNodes().map(p => [p.key, p.text]))));

        return diag;
    }

    /**
     * 최대 선택 수 제한: 1 이면 다른 체크를 해제하고, 그 외에는 초과한 체크를 되돌린 뒤 안내한다.
     * @param input 방금 변경된 체크상자
     * @param treeEl 트리 영역
     * @param max 최대 선택 수 (0 이면 무제한)
     */
    private limit(input: HTMLInputElement, treeEl: HTMLElement, max: number) {
        if (!input.checked || max <= 0) return;
        const checked = [...treeEl.querySelectorAll<HTMLInputElement>("input[type=\"checkbox\"]:checked")];
        if (max === 1) checked.forEach(p => { if (p !== input) p.checked = false; });
        else if (checked.length > max) {
            input.checked = false;
            window["common"].alert((this.langs["MaxSelectMsg"] ?? "").replace("{0}", String(max)));
        }
    }

    /**
     * 선택 불가 키를 disabled 로 표시한다. withChildren 이면 그 하위 노드 전체도 함께 표시한다.
     * @param nodes 트리 노드 목록
     * @param keys 선택 불가 키 목록
     * @param withChildren 하위 노드까지 선택 불가 처리 여부
     * @param inherited 상위 노드가 하위까지 선택 불가인지 여부
     */
    private static disable(nodes: ITreeNode[], keys: string[], withChildren: boolean, inherited: boolean) {
        nodes.forEach(p => {
            const disabled = inherited || keys.includes(p.key);
            if (disabled) p.disabled = true;
            if (p.child) GroupSelect.disable(p.child, keys, withChildren, withChildren && disabled);
        });
    }
}
