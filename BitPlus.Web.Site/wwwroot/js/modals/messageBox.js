/**
 * Alert / Info / Confirm 표준 메시지 박스 모달
 */
export default class MessageBox {
    langs = {};
    /**
     * 다국어 사전 비동기 로드
     */
    async loadAsync(args) {
        if (window["common"] && typeof window["common"].getLangsAsync === "function") {
            this.langs = await window["common"].getLangsAsync("Cancel, Confirm");
        }
    }
    /**
     * 대화상자 DOM 생성
     */
    render(args) {
        const messageType = args["messageType"] || "alert";
        let iconTag = "";
        let defaultTitle = "";
        let isConfirm = false;
        switch (messageType) {
            case "confirm":
                iconTag = '<i class="fi fiChatHelp"></i>';
                defaultTitle = "Confirm";
                isConfirm = true;
                break;
            case "info":
                iconTag = '<i class="fi fiAlertOn"></i>';
                defaultTitle = "Information";
                break;
            default:
                iconTag = '<i class="fi fiChatWarning"></i>';
                defaultTitle = "Alert";
                break;
        }
        const title = args["title"] || defaultTitle;
        const content = args["content"] || "";
        const cancelText = this.langs["Cancel"] || "Cancel";
        const confirmText = this.langs["Confirm"] || "Confirm";
        const diag = document.createElement("dialog");
        diag.classList.add(`${messageType}Win`);
        diag.innerHTML = `
            <header>
                ${iconTag}
                <span>${title}</span>
            </header>
            <div>${content}</div>
            <footer>
                ${isConfirm ? `<button type="button" class="cancelBtn"><i class="fi fiProhibit"></i> ${cancelText}</button>` : ""}
                <button type="button" class="primary confirmBtn"><i class="fi fiCheckmark"></i> ${confirmText}</button>
            </footer>
        `;
        const cancelBtn = diag.querySelector(".cancelBtn");
        if (cancelBtn) {
            cancelBtn.addEventListener("click", () => {
                if (diag["closer"])
                    diag["closer"](false);
                else
                    diag.close("false");
            });
        }
        const confirmBtn = diag.querySelector(".confirmBtn");
        if (confirmBtn) {
            confirmBtn.addEventListener("click", () => {
                if (diag["closer"])
                    diag["closer"](true);
                else
                    diag.close("true");
            });
        }
        return diag;
    }
}
//# sourceMappingURL=messageBox.js.map