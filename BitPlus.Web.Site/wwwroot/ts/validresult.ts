/**
 * 유효성 검사 결과 모델. 서버 ValidAttribute 실패 응답(Result.Fail + Messages)과 동일한 형태를 클라이언트에서도 사용한다.
 */
export default class ValidResult {
    success: boolean;
    messages: Record<string, string>;

    constructor(success: boolean, messages: Record<string, string> = {}) {
        this.success = success;
        this.messages = messages;
    }

    /**
     * 실패 메시지를 popover 토스트로 일괄 표시한다(필드별 인라인 표시가 아닌 전체 폼 단위 안내).
     * @param showSec 자동 닫힘까지의 표시 시간(초)
     * @param maxCount 한 번에 표시할 최대 메시지 개수
     * @returns 체이닝용 자기 자신 (예: result.popover().markErrors(form))
     */
    popover(showSec: number = 5, maxCount: number = 3): ValidResult {
        const msgs = Object.values(this.messages).slice(0, maxCount);
        if (msgs.length === 0) return this;

        const box = document.createElement("div");
        box.popover = "manual";
        box.className = "danger validMsg";
        box.innerHTML = `<i class="fi fiExclamationTriangle"></i><div><header>Valid Error</header><ul>${msgs.map(m => `<li>${m}</li>`).join("")}</ul></div>`;

        const container = document.getElementById("messages") || document.body;
        container.appendChild(box);

        try { box.showPopover(); } catch { box.style.display = "block"; }

        // hidePopover 후 CSS 퇴장 트랜지션(위로 사라짐)이 끝나면 제거
        setTimeout(() => {
            try { box.hidePopover(); } catch { }
            Promise.all(box.getAnimations().map(a => a.finished)).finally(() => box.remove());
        }, showSec * 1000);
        return this;
    }

    /**
     * 실패한 필드와 같은 name 의 입력항을 영역에서 찾아 error 클래스로 오류 상태를 표시한다.
     * 입력항이 입력 그룹(.inputGroup) 안에 있으면 입력 그룹도 오류 상태로 표시한다. 이전 오류 표시는 먼저 지운다.
     * 첫 오류 입력항이 숨은 탭(.tabBox) 안에 있으면 그 탭으로 전환하고 포커스를 준다.
     * 오류 표시는 해당 입력항을 다시 입력하거나 모달이 닫히면 site.ts 전역 처리로 해제된다.
     * @param container 입력항을 찾을 영역 (폼, 모달 등)
     * @returns 체이닝용 자기 자신
     */
    markErrors(container: ParentNode): ValidResult {
        ValidResult.clearErrors(container);
        const inputs = Object.keys(this.messages)
            .map(name => container.querySelector<HTMLElement>(`[name="${name}"]`))
            .filter((p): p is HTMLElement => !!p);

        inputs.forEach(input => {
            input.classList.add("error");
            input.closest(".inputGroup")?.classList.add("error");
        });
        if (inputs.length > 0) ValidResult.reveal(inputs[0]);
        return this;
    }

    /**
     * 입력항이 비활성 탭 패널 안에 있으면 해당 탭 헤더를 눌러 전환한 뒤 포커스를 준다.
     * @param input 대상 입력항
     */
    private static reveal(input: HTMLElement) {
        const panel = input.closest<HTMLElement>(".tabBox > section > *");
        if (panel?.id && !panel.classList.contains("active"))
            panel.closest(".tabBox")?.querySelector<HTMLElement>(`:scope > header > [data-for="#${panel.id}"]`)?.click();
        input.focus();
    }

    /**
     * 영역 안의 오류 상태 표시(error 클래스)를 모두 지운다.
     * @param container 대상 영역
     */
    static clearErrors(container: ParentNode) {
        container.querySelectorAll(".error").forEach(p => p.classList.remove("error"));
    }

    /**
     * 입력항 하나의 오류 상태를 지우고, 같은 입력 그룹에 남은 오류가 없으면 입력 그룹의 오류 상태도 지운다.
     * @param input 다시 입력된 입력항
     */
    static clearError(input: Element) {
        input.classList.remove("error");
        const group = input.closest(".inputGroup");
        if (group && !group.querySelector(":scope > .error")) group.classList.remove("error");
    }
}

// 인라인 페이지 스크립트에서 검증 외 오류(예: 비밀번호 확인 불일치)도 같은 방식(팝오버 + 오류 표시)으로 보이도록 전역에 노출한다.
(window as any).ValidResult = ValidResult;
