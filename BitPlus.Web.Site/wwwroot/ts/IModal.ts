declare global {
    interface HTMLDialogElement extends HTMLElement {
        open: boolean;
        returnValue: string;
        show(): void;
        showModal(): void;
        close(returnValue?: string): void;
    }
}

/**
 * 모달 대화상자 컴포넌트 표준 인터페이스
 */
export default interface IModal {
    /**
     * 모달 렌더링 전 비동기 데이터(다국어 사전 등) 로드
     * @param args 모달 전달 인자
     */
    loadAsync(args: Record<string, any>): Promise<any>;

    /**
     * 모달 <dialog> 엘리먼트 생성 및 이벤트 바인딩
     * @param args 모달 전달 인자
     * @returns 생성된 HTMLDialogElement
     */
    render(args: Record<string, any>): HTMLDialogElement;
}
