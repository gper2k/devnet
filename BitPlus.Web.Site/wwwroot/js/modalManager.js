/**
 * 모달창 외부 제어 및 강제 종료 토큰
 */
export class CloseToken {
    target = null;
    set = (diag) => this.target = diag;
    close = () => { if (this.target && this.target["closer"])
        this.target["closer"](); };
}
/**
 * 동적 모달 라이프사이클 관리자
 */
export default class ModalManager {
    wrap;
    /**
     * @param wrapSelector 모달이 삽입될 DOM 컨테이너 셀렉터 (기본값: "#modals")
     */
    constructor(wrapSelector = "#modals") {
        this.wrap = document.querySelector(wrapSelector) || document.body;
    }
    /** 외부 제어용 닫기 토큰 생성 */
    makeCloseToken = () => new CloseToken();
    /**
     * 동적 모달 컴포넌트를 로드하고 표시
     * @param name 모달 컴포넌트 명칭 (예: "messageBox" 또는 "MessageBox")
     * @param args 모달에 전달할 인자 객체
     * @param closeToken 외부에서 닫기를 제어할 토큰 (선택)
     * @returns 모달 반환값 Promise
     */
    append(name, args = {}, closeToken) {
        return new Promise(async (resolve, reject) => {
            try {
                const fileName = name.charAt(0).toLowerCase() + name.slice(1);
                const module = await import(`/js/modals/${fileName}.js`);
                const ModalClass = module.default;
                const modal = new ModalClass(args);
                await modal.loadAsync(args);
                const diag = modal.render(args);
                this.wrap.append(diag);
                closeToken?.set(diag);
                diag.showModal();
                // closer(result) 로 닫으면 result 를 형식 그대로(객체 포함) 반환, Esc 등으로 닫히면 returnValue 문자열 기반 반환
                let closeResult = undefined;
                diag["closer"] = (result) => {
                    closeResult = result;
                    if (!diag.classList.contains("closing"))
                        diag.classList.add("closing");
                    setTimeout(() => diag.close(result !== undefined ? String(result) : ""), 240);
                };
                diag.addEventListener("close", () => {
                    const returnVal = diag.returnValue;
                    let parsedVal = returnVal;
                    if (closeResult !== undefined)
                        parsedVal = closeResult;
                    else if (returnVal === "true")
                        parsedVal = true;
                    else if (returnVal === "false")
                        parsedVal = false;
                    resolve(parsedVal);
                    diag.remove();
                });
            }
            catch (error) {
                reject(error);
            }
        });
    }
}
//# sourceMappingURL=modalManager.js.map