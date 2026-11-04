import IModal from "../IModal.js";

/**
 * 사이드 구성 모달 인자 규격
 */
export interface ISideEditArgs {
    /** 사이드 감춤 여부 */
    hidden?: boolean;
    /** 사이드 위치 ("Left" 기본, "Right") */
    position?: string;
    /** 연결 메뉴 키 (빈 값이면 자동) */
    menuKey?: string;
    /** 연결 메뉴 선택 목록 (사용자 메뉴 트리, depth 만큼 들여쓰기) */
    menus?: { key: string; text: string; depth: number }[];
}

/**
 * 사이드 구성 모달: 사이드 감춤, 사이드 위치(좌측/우측), 연결 메뉴(맨 위 자동 + 사용자 메뉴 트리) — 게시판 설정의 "사이드 구성"과 같은 항목.
 * common.modal.append("sideEdit", args) 로 열며, 확인 시 { hidden, position, menuKey }, 취소/Esc 시 null 로 닫힌다. 저장은 부르는 쪽이 한다.
 */
export default class SideEdit implements IModal {
    private langs: Record<string, string>;

    /**
     * SideEdit 기본 생성자
     */
    constructor() {
        this.langs = {};
    }

    /**
     * 모달 문구 다국어 사전 비동기 로드
     */
    async loadAsync(args: Record<string, any>): Promise<any> {
        this.langs = await window["common"].getLangsAsync("Cancel, Apply, SideConfig, HideSide, SidePosition, Left, Right, LinkedMenu, Auto");
    }

    /**
     * 대화상자 DOM 생성 및 이벤트 바인딩
     */
    render(args: Record<string, any>): HTMLDialogElement {
        const l = this.langs;
        const a = args as ISideEditArgs;
        const diag = document.createElement("dialog");
        diag.className = "sideEdit";
        diag.innerHTML = `
            <header><i class="fi fiPanelLeft"></i> <span>${l["SideConfig"]}</span></header>
            <section>
                <ul class="form">
                    <li><label class="check"><input type="checkbox" name="hidden" /><span>${l["HideSide"]}</span></label></li>
                    <li class="col2">
                        <div>
                            <header>${l["SidePosition"]}</header>
                            <select name="position"><option value="Left">${l["Left"]}</option><option value="Right">${l["Right"]}</option></select>
                        </div>
                        <div>
                            <header>${l["LinkedMenu"]}</header>
                            <select name="menuKey"><option value="">${l["Auto"]}</option></select>
                        </div>
                    </li>
                </ul>
            </section>
            <footer>
                <button type="button" data-mode="cancel"><i class="fi fiProhibit"></i> ${l["Cancel"]}</button>
                <button type="button" class="main" data-mode="apply"><i class="fi fiCheckmark"></i> ${l["Apply"]}</button>
            </footer>
        `;
        const hidden = diag.querySelector<HTMLInputElement>("[name=\"hidden\"]")!;
        const position = diag.querySelector<HTMLSelectElement>("[name=\"position\"]")!;
        const menu = diag.querySelector<HTMLSelectElement>("[name=\"menuKey\"]")!;
        // 메뉴 이름은 HTML 이 아닌 글자로 넣는다 (단계별 들여쓰기는 줄바꿈 없는 공백)
        (a.menus ?? []).forEach(p => menu.add(new Option("  ".repeat(p.depth) + p.text, p.key)));
        hidden.checked = !!a.hidden;
        position.value = a.position === "Right" ? "Right" : "Left";
        menu.value = a.menuKey ?? "";
        if (menu.selectedIndex < 0) menu.value = ""; // 저장된 메뉴가 지워졌으면 자동

        diag.querySelector<HTMLElement>("[data-mode=\"cancel\"]")!.onclick = () => diag["closer"](null);
        diag.querySelector<HTMLElement>("[data-mode=\"apply\"]")!.onclick = () => diag["closer"]({ hidden: hidden.checked, position: position.value, menuKey: menu.value });
        return diag;
    }
}
