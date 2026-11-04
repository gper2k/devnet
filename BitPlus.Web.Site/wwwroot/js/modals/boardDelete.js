/**
 * 게시판 삭제 확인 모달: common.modal.append("boardDelete", { ruleNo }) 로 열며, 삭제를 요청하면 true, 닫으면 null 로 닫힌다.
 * 딸린 데이터 수(api/content/settings/delete)를 보여 주고, 같은 형식의 다른 게시판이 있으면 글을 그 게시판으로 옮긴 뒤 지울지 고르게 한다.
 * 영구 삭제 확인을 체크해야 삭제 버튼이 켜지며, 삭제는 DELETE api/content/settings?ruleNo=&moveTo= 로 요청한다 (게시판은 바로 비활성, 정리는 서버가 이어서 함).
 * 모양은 추가 확인 창(checkConfirm) 스타일을 그대로 쓴다.
 */
export default class BoardDelete {
    langs;
    info;
    /**
     * BoardDelete 기본 생성자
     */
    constructor() {
        this.langs = {};
        this.info = null;
    }
    /**
     * 문구 다국어 사전과 삭제 확인 정보 로드 (정보를 못 받으면 창을 열지 않고 닫힘 처리는 render 가 함)
     */
    async loadAsync(args) {
        const common = window["common"];
        [this.langs, this.info] = await Promise.all([
            common.getLangsAsync("Cancel, Delete, DelMsg, BoardDeleteInfo, BoardDeleteCounts, BoardDeleteMove, BoardDeleteOnly, BoardDeleteAgree, SubMenus"),
            common.getSend(`/api/content/settings/delete?ruleNo=${args.ruleNo}`).then((res) => res?.success ? res.data : null)
        ]);
    }
    /**
     * 대화상자 DOM 생성 (경고 제목 + 딸린 데이터 수 + 안내 + 글 처리 선택 + 영구 삭제 확인 + 취소/삭제)
     */
    render(args) {
        const common = window["common"];
        const t = this.langs;
        const info = this.info;
        const diag = document.createElement("dialog");
        diag.className = "checkConfirm";
        if (!info) {
            setTimeout(() => diag["closer"](null)); // 조회 실패(권한 없음 등)는 getSend 가 이미 알림을 띄웠음
            return diag;
        }
        const targets = info.targets ?? [];
        const counts = t["BoardDeleteCounts"].replace("{0}", info.posts).replace("{1}", info.comments).replace("{2}", info.files)
            .replace("{3}", common.fileSizeText(info.fileSize)).replace("{4}", info.reports);
        diag.innerHTML = String(common.html `
            <header><i class="fi fiDelete"></i> <span>${t["Delete"]}</span></header>
            <div>
                <p>${t["DelMsg"].replace("{0}", info.title)}<br />${counts}${info.subMenus > 0 ? ` · ${t["SubMenus"]} ${info.subMenus}` : ""}</p>
                <p>${t["BoardDeleteInfo"]}</p>
                <ul></ul>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${t["Cancel"]}</button>
                <button type="button" class="primary" disabled><i class="fi fiDelete"></i> ${t["Delete"]}</button>
            </footer>
        `);
        const list = diag.querySelector("ul");
        if (info.canMove && targets.length > 0 && info.posts > 0) {
            list.insertAdjacentHTML("beforeend", String(common.html `
                <li><label class="radio"><input type="radio" name="boardDeleteMode" value="delete" checked /><span>${t["BoardDeleteOnly"]}</span></label></li>
                <li><label class="radio"><input type="radio" name="boardDeleteMode" value="move" /><span>${t["BoardDeleteMove"]}</span></label></li>
                <li><select name="moveTo" disabled>${targets.map(p => common.html `<option value="${p.ruleNo}">${p.title}</option>`)}</select></li>
            `));
        }
        list.insertAdjacentHTML("beforeend", String(common.html `<li><label class="check danger"><input type="checkbox" name="agree" /><span>${t["BoardDeleteAgree"]}</span></label></li>`));
        const moveTo = list.querySelector("select");
        const agree = list.querySelector("[name=agree]");
        const [cancelBtn, deleteBtn] = diag.querySelectorAll("footer > button");
        const moving = () => list.querySelector("[name=boardDeleteMode]:checked")?.value === "move";
        list.addEventListener("change", () => {
            if (moveTo)
                moveTo.disabled = !moving();
            deleteBtn.disabled = !agree.checked;
        });
        cancelBtn.addEventListener("click", () => diag["closer"](null));
        deleteBtn.addEventListener("click", async () => {
            const query = new URLSearchParams({ ruleNo: String(info.ruleNo) });
            if (moveTo && moving())
                query.append("moveTo", moveTo.value);
            const res = await common.delSend(`/api/content/settings?${query}`);
            if (res?.success)
                diag["closer"](true); // 결과 문구는 delSend 가 알림으로 보여 줌
        });
        return diag;
    }
}
//# sourceMappingURL=boardDelete.js.map