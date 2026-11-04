/* ==========================================================================
   DateTimePick: 날짜·시간 입력 컨트롤 — 태그 선언만으로 쓴다 (Site.ts 하단에서 초기화·관찰)

   <div class="datePick"     data-name="day"  data-value="2026-10-14"        data-min="2026-01-01" data-max="2026-12-31" data-placeholder="날짜"></div>
   <div class="timePick"     data-name="time" data-value="09:30"             data-step="5"></div>
   <div class="dateTimePick" data-name="start" data-value="2026-10-14T09:30"></div>

   - 값(value 속성, 사용자의 로컬 시간): 날짜 yyyy-MM-dd / 시간 HH:mm / 날짜시간 yyyy-MM-ddTHH:mm, 비어 있으면 빈 문자열
     (요소의 value 를 읽고 쓰며, data-name 이 있으면 같은 이름의 hidden input 도 함께 갱신되어 폼 전송에 쓸 수 있다)
   - 사용자가 값을 바꾸면 요소에서 change 이벤트(bubbles)가 발생한다.
   - 날짜는 달력 팝업(월 이동, 오늘, 초기화 — 날짜를 누르면 바로 값이 정해짐), 시간은 시/분 스피너 팝업(위/아래 화살표·직접 입력, data-step 분 단위 간격 — 적용 버튼 또는 입력칸에서 Enter 를 눌러야 값이 정해지고, 초기화는 값을 비움).
   - dateTimePick 은 날짜와 시간을 따로 입력받는 한 쌍이며, 날짜만 고르면 시간은 현재 시(분 00)입니다. 시간 팝업도 값이 없으면 현재 시(분 00)에서 시작합니다.
   ========================================================================== */

const p2 = (n: number) => String(n).padStart(2, "0");

/** 화면 언어 (common.culture, 없으면 문서 lang, 기본 ko) */
const culture = (): string => window["common"]?.culture || document.documentElement.lang || "ko";

/** Date → yyyy-MM-dd (로컬) */
const toDateStr = (d: Date) => `${d.getFullYear()}-${p2(d.getMonth() + 1)}-${p2(d.getDate())}`;

/** yyyy-MM-dd(로 시작하는 문자열) → 로컬 Date, 잘못된 날짜면 null */
function parseDate(text?: string | null): Date | null {
    const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(text ?? "");
    if (!m) return null;
    const d = new Date(+m[1], +m[2] - 1, +m[3]);
    return d.getMonth() === +m[2] - 1 ? d : null;
}

/** 시간을 처음 정할 때의 기본값: 현재 시, 분은 00 (HH:mm) */
const defaultTime = () => `${p2(new Date().getHours())}:00`;

/** HH:mm → [시, 분], 잘못된 값이면 null */
function parseTime(text?: string | null): [number, number] | null {
    const m = /^(\d{1,2}):(\d{2})/.exec(text ?? "");
    if (!m) return null;
    const h = +m[1], mi = +m[2];
    return h < 24 && mi < 60 ? [h, mi] : null;
}

// ─────────────── 팝업 (한 번에 하나) ───────────────
let popup: HTMLElement | null = null;
let popupOwner: HTMLElement | null = null;
let stopFollow: (() => void) | null = null; // 스크롤·크기 변경 때 팝업이 입력 항목을 따라가게 하는 리스너 해제

/** 열려 있는 팝업을 닫는다. */
export function closePicker() {
    stopFollow?.();
    stopFollow = null;
    popup?.remove();
    popup = null;
    popupOwner = null;
}

// 팝업 바깥 클릭/Esc 로 닫는다 (Esc 는 모달까지 닫지 않도록 기본 동작을 막는다)
// (클릭으로 달력을 다시 그리면 눌린 버튼이 문서에서 빠지므로, target 의 소속이 아니라 클릭 시점의 이벤트 경로로 판단한다)
document.addEventListener("click", e => {
    if (!popup) return;
    const path = e.composedPath();
    if (!path.includes(popup) && !(popupOwner && path.includes(popupOwner))) closePicker();
});
document.addEventListener("keydown", e => { if (e.key === "Escape" && popup) { e.preventDefault(); e.stopPropagation(); closePicker(); } }, true);

/**
 * 기준 요소 아래에 팝업을 띄운다. 모달(dialog) 안의 요소여도 가려지거나 잘리지 않도록 그 dialog 안에 Popover API(최상위 레이어)로 띄우고,
 * 화면 밖으로 나가면 위치를 보정한다. 지원하지 않는 브라우저는 본문에 붙인 절대 위치로 대신한다.
 */
function openPopup(owner: HTMLElement, content: HTMLElement) {
    closePicker();
    popup = content;
    popupOwner = owner;
    content.classList.add("pickPopup");
    const native = typeof content.showPopover === "function";
    if (native) content.popover = "manual";
    // 모달(dialog) 안의 컨트롤이면 팝업도 그 dialog 안에 붙인다: 모달이 열려 있으면 바깥 요소는 비활성(inert)이라 클릭할 수 없고,
    // 최상위 레이어(popover)는 dialog 의 overflow 로 잘리지도 않는다.
    (owner.closest("dialog") ?? document.body).appendChild(content);
    if (native) content.showPopover();

    const place = () => {
        const r = owner.getBoundingClientRect();
        const w = content.offsetWidth, h = content.offsetHeight;
        let left = r.left, top = r.bottom + 4;
        if (left + w > window.innerWidth) left = Math.max(0, window.innerWidth - w - 4);
        if (top + h > window.innerHeight && r.top - h - 4 >= 0) top = r.top - h - 4;
        const sx = native ? 0 : window.scrollX, sy = native ? 0 : window.scrollY;
        content.style.left = `${left + sx}px`;
        content.style.top = `${top + sy}px`;
    };
    place();
    // 최상위 레이어(popover)는 화면 기준 위치라서, 스크롤(어느 스크롤 영역이든)·창 크기가 바뀌면 입력 항목 위치에 맞춰 다시 놓는다
    window.addEventListener("scroll", place, true);
    window.addEventListener("resize", place);
    stopFollow = () => { window.removeEventListener("scroll", place, true); window.removeEventListener("resize", place); };
}

/** 컨트롤에 값 변경(change) 이벤트를 알린다. */
const fire = (el: HTMLElement) => el.dispatchEvent(new Event("change", { bubbles: true }));

/** 이름(data-name)이 있으면 값을 담는 hidden input 을 만든다. */
function makeHidden(el: HTMLElement): HTMLInputElement | null {
    if (!el.dataset.name) return null;
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = el.dataset.name;
    el.appendChild(input);
    return input;
}

// ─────────────── 날짜 ───────────────
/** .datePick 초기화: 트리거(달력 아이콘 + 선택한 날짜) + 달력 팝업 */
function initDate(el: HTMLElement) {
    let value = "";
    const min = parseDate(el.dataset.min), max = parseDate(el.dataset.max);
    el.innerHTML = `<button type="button" class="pickTrigger"><i class="fi fiCal"></i><span class="pickText"></span><i class="fi fiChevronDown pickArrow"></i></button>`;
    const trigger = el.querySelector<HTMLButtonElement>(".pickTrigger")!;
    const text = el.querySelector<HTMLElement>(".pickText")!;
    const hidden = makeHidden(el);

    const render = () => {
        const d = parseDate(value);
        text.textContent = d ? d.toLocaleDateString(culture(), { year: "numeric", month: "long", day: "numeric" }) : (el.dataset.placeholder ?? "");
        text.classList.toggle("placeholder", !d);
        if (hidden) hidden.value = value;
    };
    const set = (next: string, notify: boolean) => {
        const d = parseDate(next);
        value = d ? toDateStr(d) : "";
        render();
        if (notify) fire(el);
    };
    Object.defineProperty(el, "value", { get: () => value, set: (v: string) => set(v, false), configurable: true });
    set(el.dataset.value ?? "", false);

    trigger.onclick = () => {
        if (popup && popupOwner === el) return closePicker();
        const selected = parseDate(value);
        let view = new Date((selected ?? new Date()).getFullYear(), (selected ?? new Date()).getMonth(), 1);
        const box = document.createElement("div");
        box.className = "pickCalendar";
        // 화면: days(날짜) → 년도를 누르면 years(10년 단위 12칸), 월을 누르면 months(12칸). 년도/월은 문화권과 상관없이 4자리 년도, 2자리 월 숫자로 보이고 년·월 글자 대신 슬래시(/)로 나눈다.
        let mode: "days" | "months" | "years" = "days";
        const year4 = (y: number) => String(y).padStart(4, "0");
        const nav = (dir: string) => `<button type="button" data-nav="${dir}"><i class="fi fi${dir === "-1" ? "ChevronLeft" : "ChevronRight"}"></i></button>`;
        const cell = (label: string, attrs: string, cls: string, off: boolean) => `<button type="button" ${attrs}${off ? " disabled" : ""} class="${cls}">${label}</button>`;
        const drawYears = () => {
            const decade = Math.floor(view.getFullYear() / 10) * 10;
            const cells = Array.from({ length: 12 }, (_, i) => decade - 1 + i).map(y => cell(year4(y), `data-year="${y}"`,
                [y === selected?.getFullYear() ? "selected" : "", y === new Date().getFullYear() ? "today" : "", y < decade || y > decade + 9 ? "other" : ""].join(" ").trim(),
                y < 1 || y > 9999 || (!!min && y < min.getFullYear()) || (!!max && y > max.getFullYear())));
            return `<header>${nav("-1")}<strong>${year4(decade)} - ${year4(decade + 9)}</strong>${nav("1")}</header><div class="pickGrid">${cells.join("")}</div>`;
        };
        const drawMonths = () => {
            const y = view.getFullYear();
            const cells = Array.from({ length: 12 }, (_, m) => cell(p2(m + 1), `data-month="${m}"`,
                [y === selected?.getFullYear() && m === selected.getMonth() ? "selected" : "", y === new Date().getFullYear() && m === new Date().getMonth() ? "today" : ""].join(" ").trim(),
                (!!min && new Date(y, m + 1, 0) < min) || (!!max && new Date(y, m, 1) > max)));
            return `<header>${nav("-1")}<strong><button type="button" data-view="years">${year4(y)}</button></strong>${nav("1")}</header><div class="pickGrid">${cells.join("")}</div>`;
        };
        const draw = () => {
            if (mode !== "days") {
                box.innerHTML = (mode === "years" ? drawYears() : drawMonths())
                    + `<footer><button type="button" data-today title="Today"><i class="fi fiCal"></i></button><button type="button" data-clear><i class="fi fiArrReset"></i></button></footer>`;
                return;
            }
            const today = toDateStr(new Date());
            const start = new Date(view.getFullYear(), view.getMonth(), 1 - view.getDay()); // 그 달 1일이 든 주의 일요일부터
            const weekdays = Array.from({ length: 7 }, (_, i) => new Date(2023, 0, 1 + i).toLocaleDateString(culture(), { weekday: "short" }));
            const days = Array.from({ length: 42 }, (_, i) => new Date(start.getFullYear(), start.getMonth(), start.getDate() + i));
            box.innerHTML = `<header><button type="button" data-nav="-1"><i class="fi fiChevronLeft"></i></button><strong><button type="button" data-view="years">${year4(view.getFullYear())}</button><span>/</span><button type="button" data-view="months">${p2(view.getMonth() + 1)}</button></strong><button type="button" data-nav="1"><i class="fi fiChevronRight"></i></button></header>`
                + `<div class="pickDays">${weekdays.map(w => `<span>${w}</span>`).join("")}`
                + days.map(d => {
                    const s = toDateStr(d);
                    const off = (min && d < min) || (max && d > max);
                    const other = d.getMonth() !== view.getMonth();
                    return `<button type="button" data-day="${s}"${off ? " disabled" : ""} class="${[s === value ? "selected" : "", s === today ? "today" : "", other ? "other" : ""].join(" ").trim()}">${d.getDate()}</button>`;
                }).join("") + `</div>`
                + `<footer><button type="button" data-today title="Today"><i class="fi fiCal"></i></button><button type="button" data-clear><i class="fi fiArrReset"></i></button></footer>`;
        };
        box.onclick = e => {
            const t = (e.target as HTMLElement).closest("button");
            if (!t) return;
            const dir = Number(t.dataset.nav);
            if (t.dataset.nav) { view = mode === "days" ? new Date(view.getFullYear(), view.getMonth() + dir, 1) : new Date(view.getFullYear() + dir * (mode === "years" ? 10 : 1), view.getMonth(), 1); draw(); }
            else if (t.dataset.view) { mode = t.dataset.view as "months" | "years"; draw(); }
            else if (t.dataset.year) { view = new Date(Number(t.dataset.year), view.getMonth(), 1); mode = "months"; draw(); }
            else if (t.dataset.month) { view = new Date(view.getFullYear(), Number(t.dataset.month), 1); mode = "days"; draw(); }
            else if (t.dataset.day) { set(t.dataset.day, true); closePicker(); }
            else if (t.hasAttribute("data-today")) { const n = new Date(); if ((!min || n >= min) && (!max || n <= max)) { set(toDateStr(n), true); closePicker(); } }
            else if (t.hasAttribute("data-clear")) { set("", true); closePicker(); }
        };
        draw();
        openPopup(el, box);
        Object.assign(box.style, { minWidth: `${box.offsetWidth}px`, minHeight: `${box.offsetHeight}px` }); // 날짜 화면(6주) 크기를 기준으로, 년도·월 화면으로 바꿔도 팝업 너비·높이가 변하지 않게
    };
}

// ─────────────── 시간 ───────────────
/** .timePick 초기화: 트리거(시계 아이콘 + HH : mm) + 시/분 스피너 팝업 */
function initTime(el: HTMLElement) {
    let value = "";
    const step = Math.max(1, Math.min(30, Number(el.dataset.step) || 1));
    el.innerHTML = `<button type="button" class="pickTrigger"><i class="fi fiClock"></i><span class="pickText"></span><i class="fi fiChevronDown pickArrow"></i></button>`;
    const trigger = el.querySelector<HTMLButtonElement>(".pickTrigger")!;
    const text = el.querySelector<HTMLElement>(".pickText")!;
    const hidden = makeHidden(el);

    const render = () => {
        const t = parseTime(value);
        text.textContent = t ? `${p2(t[0])} : ${p2(t[1])}` : (el.dataset.placeholder ?? "-- : --");
        text.classList.toggle("placeholder", !t);
        if (hidden) hidden.value = value;
    };
    const set = (next: string, notify: boolean) => {
        const t = parseTime(next);
        value = t ? `${p2(t[0])}:${p2(t[1])}` : "";
        render();
        if (notify) fire(el);
    };
    Object.defineProperty(el, "value", { get: () => value, set: (v: string) => set(v, false), configurable: true });
    set(el.dataset.value ?? "", false);

    trigger.onclick = () => {
        if (popup && popupOwner === el) return closePicker();
        let [h, m] = parseTime(value) ?? parseTime(defaultTime())!; // 값이 없으면 현재 시(분 00)에서 시작
        const box = document.createElement("div");
        box.className = "pickTime";
        const col = (kind: "h" | "m", label: string) => `<div><small>${label}</small><button type="button" data-spin="${kind}" data-dir="1"><i class="fi fiChevronUp"></i></button><input type="text" inputmode="numeric" maxlength="2" data-box="${kind}" /><button type="button" data-spin="${kind}" data-dir="-1"><i class="fi fiChevronDown"></i></button></div>`;
        box.innerHTML = col("h", "Hour") + `<b>:</b>` + col("m", "Minute") + `<footer><button type="button" data-apply><i class="fi fiCheckmark"></i></button><button type="button" data-clear><i class="fi fiArrReset"></i></button></footer>`;
        const boxH = box.querySelector<HTMLInputElement>("[data-box=\"h\"]")!;
        const boxM = box.querySelector<HTMLInputElement>("[data-box=\"m\"]")!;
        const show = () => { boxH.value = p2(h); boxM.value = p2(m); };
        // 시·분은 팝업 안에서만 바뀌고, 적용 버튼(또는 입력칸에서 Enter)을 눌러야 값으로 정해진다. 적용하지 않고 닫으면 버려진다.
        const readBox = (input: HTMLInputElement, max: number) => Math.min(max, Number(input.value) || 0);
        const apply = () => {
            h = readBox(boxH, 23);
            m = readBox(boxM, 59);
            set(`${p2(h)}:${p2(m)}`, true);
            closePicker();
        };
        const spin = (kind: string, dir: number) => {
            if (kind === "h") h = (h + dir + 24) % 24;
            else m = (m + dir * step + 60) % 60;
            show();
        };
        box.onclick = e => {
            const t = (e.target as HTMLElement).closest("button");
            if (!t) return;
            if (t.dataset.spin) spin(t.dataset.spin, Number(t.dataset.dir));
            else if (t.hasAttribute("data-apply")) apply();
            else if (t.hasAttribute("data-clear")) { set("", true); closePicker(); }
        };
        // 직접 입력: 숫자만, 범위로 보정, 위/아래 화살표 키로도 증감
        [[boxH, "h", 23], [boxM, "m", 59]].forEach(([input, kind, max]) => {
            const field = input as HTMLInputElement;
            field.oninput = () => { field.value = field.value.replace(/\D/g, ""); };
            field.onchange = () => {
                const n = readBox(field, max as number);
                if (kind === "h") h = n; else m = n;
                show();
            };
            field.onkeydown = e => {
                if (e.key === "ArrowUp" || e.key === "ArrowDown") { e.preventDefault(); spin(kind as string, e.key === "ArrowUp" ? 1 : -1); }
                else if (e.key === "Enter") { e.preventDefault(); apply(); }
            };
            field.onfocus = () => field.select();
        });
        show();
        openPopup(el, box);
    };
}

// ─────────────── 날짜 + 시간 ───────────────
/** .dateTimePick 초기화: 날짜와 시간을 따로 입력받는 한 쌍 (값 yyyy-MM-ddTHH:mm, 날짜만 고르면 시간은 현재 시(분 00)) */
function initDateTime(el: HTMLElement) {
    const [date, time] = ["datePick", "timePick"].map(name => {
        const child = document.createElement("div");
        child.className = name;
        child.dataset.pick = "1"; // 관찰자가 다시 초기화하지 않게
        el.appendChild(child);
        return child;
    });
    if (el.dataset.placeholder) date.dataset.placeholder = el.dataset.placeholder;
    if (el.dataset.min) date.dataset.min = el.dataset.min.slice(0, 10);
    if (el.dataset.max) date.dataset.max = el.dataset.max.slice(0, 10);
    if (el.dataset.step) time.dataset.step = el.dataset.step;
    initDate(date);
    initTime(time);
    const hidden = makeHidden(el);

    const read = () => (date as any).value ? `${(date as any).value}T${(time as any).value || defaultTime()}` : "";
    const sync = () => { if (hidden) hidden.value = read(); };
    Object.defineProperty(el, "value", {
        get: read,
        set: (v: string) => {
            const [d, t] = (v ?? "").split("T");
            (date as any).value = d ?? "";
            (time as any).value = d ? (t ?? "") : "";
            sync();
        },
        configurable: true
    });
    el.addEventListener("change", e => {
        if (e.target === el) return;
        e.stopPropagation();
        if ((date as any).value && !(time as any).value) (time as any).value = defaultTime(); // 날짜만 고르면 시간은 현재 시(분 00)
        if (!(date as any).value) (time as any).value = ""; // 날짜를 지우면 시간도 비움
        sync();
        fire(el);
    });
    (el as any).value = el.dataset.value ?? "";
}

/**
 * 범위 안의 날짜·시간 컨트롤(.datePick, .timePick, .dateTimePick)을 초기화한다 (이미 초기화한 것은 건너뜀).
 * 태그가 페이지에 있거나 나중에 추가돼도 Site.ts 가 호출하므로 보통 직접 부를 필요는 없고, 모달처럼 아직 문서에 붙지 않은 DOM 에서 바로 value 를 쓸 때만 부른다.
 * @param root 찾을 범위 (자기 자신 포함)
 */
export function initPickers(root: ParentNode | Element) {
    const all: HTMLElement[] = [...(root instanceof HTMLElement && root.matches(".datePick, .timePick, .dateTimePick") ? [root] : []), ...root.querySelectorAll<HTMLElement>(".datePick, .timePick, .dateTimePick")];
    for (const el of all) {
        if (el.dataset.pick) continue;
        el.dataset.pick = "1";
        if (el.classList.contains("dateTimePick")) initDateTime(el);
        else if (el.classList.contains("datePick")) initDate(el);
        else initTime(el);
    }
}
