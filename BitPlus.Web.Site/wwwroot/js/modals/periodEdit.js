import { initPickers } from "../DateTimePick.js";
/**
 * 시작/종료 일시를 지정하는 공용 기간 설정 모달. 비워 두면 그쪽은 제한 없음입니다.
 * common.modal.append("periodEdit", args) 로 열며, 확인 시 { start, end } (datetime 은 ISO 8601 UTC, date 는 yyyy-MM-dd, time 은 HH:mm 문자열 또는 null), 취소/Esc 시 null 로 닫힌다.
 * 일시 입력은 날짜와 시간을 나눠 입력받는 컨트롤(dateTimePick)이며 사용자의 로컬 시간대로 보여 주고 UTC 로 돌려준다. (common.periodEdit(start, end) 로 바로 열 수 있다)
 */
export default class PeriodEdit {
    langs;
    /**
     * PeriodEdit 기본 생성자
     */
    constructor() {
        this.langs = {};
    }
    /**
     * 모달 문구 다국어 사전 비동기 로드
     */
    async loadAsync(args) {
        this.langs = await window["common"].getLangsAsync("Cancel, Apply, Reset, PublishPeriod, PublishStart, PublishEnd, PublishPeriodInfo, PeriodInvalid");
    }
    /**
     * 대화상자 DOM 생성 및 이벤트 바인딩
     */
    render(args) {
        const l = this.langs;
        const diag = document.createElement("dialog");
        const mode = ["date", "time"].includes(args["mode"]) ? args["mode"] : "datetime";
        const cls = { datetime: "dateTimePick", date: "datePick", time: "timePick" }[mode];
        diag.className = "periodEdit";
        diag.dataset["mode"] = mode;
        diag.innerHTML = `
            <header><i class="fi fiFill"></i> <span></span></header>
            <section>
                <ul class="form">
                    <li><header></header><div class="${cls}" data-name="start"></div></li>
                    <li><header></header><div class="${cls}" data-name="end"></div></li>
                </ul>
                <small></small>
            </section>
            <footer>
                <button type="button" data-mode="reset"><i class="fi fiArrReset"></i> ${l["Reset"]}</button>
                <button type="button" data-mode="cancel"><i class="fi fiProhibit"></i> ${l["Cancel"]}</button>
                <button type="button" class="main" data-mode="apply"><i class="fi fiCheckmark"></i> ${l["Apply"]}</button>
            </footer>
        `;
        // 호출자가 준 문구는 HTML 이 아닌 글자로 넣는다
        const a = args;
        diag.querySelector("header > i").classList.add(a.icon || "fiCalClock");
        diag.querySelector("header > span").textContent = a.title || l["PublishPeriod"];
        const labels = diag.querySelectorAll("li > header");
        labels[0].textContent = a.startLabel || l["PublishStart"];
        labels[1].textContent = a.endLabel || l["PublishEnd"];
        const info = diag.querySelector("small");
        info.textContent = a.info ?? l["PublishPeriodInfo"];
        info.hidden = !info.textContent;
        initPickers(diag); // 아직 문서에 붙기 전이므로 날짜·시간 컨트롤을 바로 초기화해 value 를 쓴다
        const start = diag.querySelector(`.${cls}[data-name="start"]`);
        const end = diag.querySelector(`.${cls}[data-name="end"]`);
        const toCtl = (v) => mode === "datetime" ? PeriodEdit.toLocal(v) : (v ?? "");
        const fromCtl = (v) => mode === "datetime" ? PeriodEdit.toUtc(v) : (v || null);
        start.value = toCtl(args["start"]);
        end.value = toCtl(args["end"]);
        diag.querySelector("[data-mode=\"reset\"]").onclick = () => start.value = end.value = "";
        diag.querySelector("[data-mode=\"cancel\"]").onclick = () => diag["closer"](null);
        diag.querySelector("[data-mode=\"apply\"]").onclick = () => {
            if (start.value && end.value && end.value < start.value)
                return window["common"].alert(l["PeriodInvalid"]);
            diag["closer"]({ start: fromCtl(start.value), end: fromCtl(end.value) });
        };
        return diag;
    }
    /**
     * UTC ISO 문자열을 날짜·시간 컨트롤 값(로컬 시간대 yyyy-MM-ddTHH:mm)으로 바꾼다.
     * @param iso ISO 8601 UTC 문자열
     */
    static toLocal(iso) {
        if (!iso)
            return "";
        const d = new Date(iso.endsWith("Z") || /[+-]\d\d:\d\d$/.test(iso) ? iso : iso + "Z");
        if (isNaN(d.getTime()))
            return "";
        const p = (n) => String(n).padStart(2, "0");
        return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
    }
    /**
     * 날짜·시간 컨트롤 값(로컬 시간대)을 UTC ISO 문자열로 바꾼다 (비었으면 null).
     * @param local 입력값
     */
    static toUtc(local) {
        return local ? new Date(local).toISOString() : null;
    }
}
//# sourceMappingURL=periodEdit.js.map