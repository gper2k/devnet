import ModalManager from "./modalManager.js";
import TreeBox from "./TreeBox.js";
import { showPopMenu } from "./PopMenu.js";
import { initPickers } from "./DateTimePick.js";
import "./validator.js"; // 전역 window.Validator 등록을 위한 사이드이펙트 임포트
import ValidResult from "./validresult.js";
import SunEditorProvider from "./editors/SunEditorProvider.js";
import JoditProvider from "./editors/JoditProvider.js";
/** common.html 템플릿이 이스케이프를 끝낸 HTML (문자열처럼 이어 붙이면 그대로 들어가고, 다른 common.html 템플릿 안에 넣어도 다시 이스케이프되지 않는다) */
export class SafeHtml {
    html;
    constructor(html) {
        this.html = html;
    }
    toString() { return this.html; }
}
/**
 * BitPlus 웹 사이트 공통 클라이언트 서비스 코어
 */
export class Site {
    wrap;
    modal;
    culture;
    defaultCulture;
    cultures;
    loginUrl;
    devMode;
    options;
    editorName;
    editorPath;
    editorProviders;
    editorLoads;
    constructor(options = {}) {
        this.options = options;
        this.culture = options.culture || "ko";
        this.defaultCulture = options.defaultCulture || this.culture;
        this.cultures = options.cultures || ["ko", "en"];
        this.loginUrl = options.loginUrl || "/Account/SignIn";
        this.devMode = options.devMode ?? false;
        // 에디터: 공급자(엔진)를 등록하고 이름·경로로 고른다. 다른 에디터로 바꾸려면 IEditorProvider 구현을 등록하고 여기의 기본 이름(또는 options.editor)과 경로만 바꾼다.
        this.editorProviders = new Map();
        this.editorLoads = new Map();
        this.registerEditor(new SunEditorProvider());
        this.registerEditor(new JoditProvider());
        // 이름(editor)을 주지 않고 경로(editorPath)만 줘도 경로의 마지막 폴더 이름이 공급자 이름으로 시작하면 그 공급자를 쓴다 (예: "/lib/jodit" → jodit)
        const folder = (options.editorPath ?? "").toLowerCase().split("/").filter(Boolean).pop() ?? "";
        const byPath = folder ? [...this.editorProviders.values()].find(p => folder.startsWith(p.name)) : undefined;
        this.editorName = [options.editor, byPath?.name].find(p => p && this.editorProviders.has(p)) ?? "jodit"; // 사이트 기본 에디터 (등록된 공급자: suneditor, jodit). 모르는 이름은 기본으로 (설정 오류로 모든 에디터 화면이 멈추지 않게)
        this.editorPath = (options.editorPath || this.editorProvider.defaultPath).replace(/\/+$/, "");
        this.modal = new ModalManager("#modals");
        this.wrap = document.querySelector("#app") || document.body;
        window["common"] = this;
    }
    /**
     * 경고 대화상자 표시
     * @param content 경고 내용
     * @param title 대화상자 제목 (선택)
     */
    alert = (content, title) => this.modal.append("messageBox", { title, content, messageType: "alert" });
    /**
     * 화면 언어(문화권) 변경: Culture 쿠키(서버 UseCulture 가 읽는 값)를 1년간 저장하고 새로고침한다.
     * @param culture 문화권 코드 (예: "ko", "en")
     */
    setCulture(culture) {
        if (!culture || culture === this.culture)
            return;
        document.cookie = `Culture=${encodeURIComponent(culture)}; path=/; max-age=31536000; samesite=lax`;
        location.reload();
    }
    /**
     * 탭 페이지(.tabView) 연결: 상단 탭(a[data-tab]) ↔ URL 해시, 탭별 조각 HTML 을 fetch 로 받아 본문(.frameBody > div)에 꽂고 탭 초기화 함수를 실행한다.
     * 해시가 없거나 모르는 탭이면 첫 탭. 조각 HTML 은 탭별로 한 번만 받는다(매번 재검증 no-cache).
     * @param root 탭 페이지 루트 (.tabView)
     * @param srcMap 탭 키 → 조각 HTML 경로 (정의 순서 = 기본 탭 우선순위)
     * @param initTab 탭 키 → 조각을 꽂은 뒤 실행할 초기화 함수
     */
    bindTabView(root, srcMap, initTab = {}) {
        const content = root.querySelector(":scope > .frameBody > div");
        const cache = {};
        let current = null;
        const load = async () => {
            const hash = location.hash.slice(1);
            const key = srcMap[hash] ? hash : Object.keys(srcMap)[0];
            if (key === current)
                return;
            current = key;
            root.querySelectorAll(":scope > .frameTabs > a").forEach(a => a.classList.toggle("active", a.dataset.tab === key));
            cache[key] ??= await (await fetch(srcMap[key], { cache: "no-cache" })).text();
            content.innerHTML = cache[key];
            initTab[key]?.();
        };
        load();
        window.addEventListener("hashchange", load);
    }
    /**
     * 일반 정보 안내 대화상자 표시
     * @param content 안내 내용
     * @param title 대화상자 제목 (선택)
     */
    info = (content, title) => this.modal.append("messageBox", { title, content, messageType: "info" });
    /**
     * 사용자 확인 대화상자 표시
     * @param content 확인 질문 내용
     * @param title 대화상자 제목 (선택)
     * @returns 확인 클릭 시 true, 취소 시 false
     */
    confirm = (content, title) => this.modal.append("messageBox", { title, content, messageType: "confirm" });
    /**
     * 문화권별 다국어 값 입력 대화상자 표시 (modals/langInput)
     * @param values 문화권별 기존 값 (Key: 문화권 코드)
     * @param title 대화상자 제목 (선택, "@"로 시작하면 다국어 리소스 키로 조회, 기본 "@LangInput")
     * @returns 적용 시 빈 값을 제외한 문화권별 값, 취소 시 null
     */
    langInput = async (values = {}, title) => {
        const result = await this.modal.append("langInput", { title, values });
        return result ? JSON.parse(result) : null;
    };
    /**
     * 다국어 입력항(input[data-lang])의 문화권별 값(JSON) 조회
     * @param input 다국어 입력항
     * @returns 문화권별 값 (형식 오류 시 빈 객체)
     */
    getLang(input) {
        try {
            return JSON.parse(input.dataset.lang || "{}");
        }
        catch {
            return {};
        }
    }
    /**
     * 다국어 입력항(input[data-lang])에 문화권별 값(JSON)을 저장하고 현재 문화권 값을 표시
     * @param input 다국어 입력항
     * @param values 문화권별 값
     */
    setLang(input, values) {
        input.dataset.lang = JSON.stringify(values);
        input.value = values[this.culture] ?? "";
        ValidResult.clearError(input); // 다국어 모달로 값을 채우면 오류 표시 해제
    }
    /**
     * 키-값 속성 상자(.keyValueBox)에 속성 목록 바인딩.
     * "@"로 시작하는 키는 시스템 속성으로 보고 "@"를 뗀 키와 읽기 전용 값(span)으로, 나머지는 값 입력 + 삭제 버튼으로 표시
     * @param box 속성 상자 루트
     * @param props 속성 (Key: 속성키, 시스템 속성은 "@" 접두)
     */
    setProps(box, props) {
        const list = box.querySelector(":scope > ul");
        list.innerHTML = "";
        Object.entries(props).forEach(([key, value]) => list.appendChild(makePropRow(key.replace(/^@/, ""), value ?? "", key.startsWith("@"))));
        this.syncEmpty(list);
    }
    /**
     * 서버 검증 실패 응답(data.messages: {필드: 메시지})의 오류 필드를 영역 안 입력항에 오류 상태로 표시한다.
     * @param res 서버 응답
     * @param container 입력항을 찾을 영역
     */
    markServerErrors(res, container) {
        const messages = res?.data?.messages;
        if (res && !res.success && messages && typeof messages === "object")
            new ValidResult(false, messages).markErrors(container);
    }
    /**
     * 칩 목록 상자(.chipBox)에 항목 바인딩 (각 칩: 기본 지정 라디오 + 삭제 버튼)
     * @param box 칩 목록 상자 루트 (data-name: 라디오 그룹명, data-modal: 추가 버튼으로 열 선택 모달명)
     * @param items 항목 (Key: 식별키, Value: 표시 텍스트)
     * @param defaultKey 기본 지정 항목 키
     */
    setChips(box, items, defaultKey) {
        const list = box.querySelector(":scope > ul");
        list.innerHTML = "";
        Object.entries(items).forEach(([key, text]) => list.appendChild(makeChip(box, key, text, key === defaultKey)));
        syncChips(list);
    }
    /**
     * 칩 목록 상자(.chipBox)의 항목 키 목록과 기본 지정 항목 키 수집
     * @param box 칩 목록 상자 루트
     * @returns keys: 항목 키 목록, defaultKey: 기본 지정 항목 키 (없으면 null)
     */
    getChips(box) {
        const items = [...box.querySelectorAll(":scope > ul > li[data-key]")];
        const checked = items.find(li => li.querySelector(":scope input[type=\"radio\"]")?.checked);
        return { keys: items.map(li => li.dataset.key), defaultKey: checked?.dataset.key ?? null };
    }
    /**
     * 태그 목록(ul.tags)의 태그 텍스트 목록 수집 (입력항이 있는 쓰기 li 제외)
     * @param list 태그 목록 ul
     * @returns 태그 텍스트 목록
     */
    getTags(list) {
        return [...list.querySelectorAll(":scope > li:not(:has(> input))")].map(li => (li.querySelector(":scope > span") ?? li).textContent.trim());
    }
    /**
     * 태그 목록(ul.tags)에 태그 바인딩 (기존 태그를 지우고 입력항 li 앞에 채운다. 입력항 li 가 없으면 읽기 전용으로 끝에 추가)
     * @param list 태그 목록 ul
     * @param tags 태그 텍스트 목록
     */
    setTags(list, tags) {
        list.querySelectorAll(":scope > li:not(:has(> input))").forEach(li => li.remove());
        const write = list.querySelector(":scope > li:has(> input)");
        tags.forEach(text => list.insertBefore(makeTag(text, !!write), write));
    }
    /**
     * 페이지 이동 버튼 목록(ul.pager) 생성. 이전/다음 + 현재 페이지 주변 번호 버튼을 만든다.
     * 클릭은 body 위임으로 처리되어 ul 에 "page" 이벤트가 발생하며, then(콜백)으로 클릭된 페이지 번호(0부터)를 받는다.
     * 예: list.after(common.makePager(0, 10, total).then(page => load(page)))
     * @param page 현재 페이지 번호 (0부터)
     * @param pageSize 페이지 크기
     * @param total 전체 목록 수
     * @param size 표시할 페이지 버튼 수 (기본 5)
     * @returns 생성된 ul.pager (then 체이닝 지원, 원하는 위치에 붙여 사용)
     */
    makePager(page, pageSize, total, size = 5) {
        const list = document.createElement("ul");
        list.className = "pager";
        const last = Math.max(0, Math.ceil(total / pageSize) - 1);
        const start = Math.max(0, Math.min(page - Math.floor(size / 2), last - size + 1));
        const pages = Array.from({ length: Math.min(size, last - start + 1) }, (_, i) => start + i);
        const item = (to, html, state) => `<li data-page="${to}"${state ? ` class="${state}"` : ""}>${html}</li>`;
        list.innerHTML = item(page - 1, `<i class="fi fiChevronLeft"></i>`, page <= 0 ? "disabled" : "")
            + pages.map(p => item(p, String(p + 1), p === page ? "active" : "")).join("")
            + item(page + 1, `<i class="fi fiChevronRight"></i>`, page >= last ? "disabled" : "");
        list.then = (callback) => {
            list.addEventListener("page", e => callback(e.detail));
            return list;
        };
        return list;
    }
    /**
     * 검색·목록 크기·페이저가 있는 관리 목록을 연결한다 (관리자 탭 조각 공용).
     * 조각 구조: header 의 input[name="search"] + 다음 버튼, .dataList > ul, footer 의 select[name="pageSize"] 와 ul.pager, header 의 i.badge(전체 수).
     * @param root 조각 루트
     * @param urlOf 목록 API 주소 (page 는 1부터, 응답 data = { items, total })
     * @param rowOf 행 하나를 만든다
     * @returns 목록 다시 불러오기 (page 는 0부터, 생략하면 현재 페이지)
     */
    bindPagedList(root, urlOf, rowOf) {
        const list = root.querySelector(".dataList > ul");
        const search = root.querySelector('input[name="search"]');
        const sizeSelect = root.querySelector('select[name="pageSize"]');
        const state = { page: 0, size: this.applyPageSize(sizeSelect), q: "" };
        const load = async (page = state.page) => {
            state.page = page;
            const res = await this.getSend(urlOf(page + 1, state.size, state.q), { useLoading: false });
            const data = (res && res.data) || { items: [], total: 0 };
            list.replaceChildren(...data.items.map(rowOf));
            this.syncEmpty(list);
            root.querySelector("header .badge").textContent = String(data.total);
            root.querySelector("footer > ul.pager").replaceWith(this.makePager(page, state.size, data.total).then(load));
        };
        const doSearch = () => { state.q = search.value.trim(); load(0); };
        search.nextElementSibling.onclick = doSearch;
        search.onkeydown = e => { if (e.key === "Enter") {
            e.preventDefault();
            doSearch();
        } };
        sizeSelect.onchange = () => { state.size = Number(sizeSelect.value); load(0); };
        load(0);
        return load;
    }
    /**
     * 관리 목록 행(li)을 한 줄 템플릿으로 만든다: cols = [[클래스, 글자 또는 노드], …] (글자는 textContent 로 넣음), data 는 li.dataset 으로.
     * @param data li 의 data-* 값
     * @param cols 열 목록
     */
    listRow(data, cols) {
        const li = document.createElement("li");
        Object.assign(li.dataset, data);
        cols.forEach(([cls, value]) => {
            const span = document.createElement("span");
            span.className = cls;
            if (value instanceof Node)
                span.append(value);
            else
                span.textContent = value == null ? "" : String(value);
            li.append(span);
        });
        return li;
    }
    /**
     * 리스트(ul/ol)의 빈 데이터 항목(li.empty) 동기화: 실제 항목이 없으면 추가하고, 있으면 제거한다.
     * 리스트에 항목을 바인딩/추가/삭제한 직후 호출한다. 문구는 다국어 리소스 "DataEmpty".
     * @param list 대상 리스트
     */
    async syncEmpty(list) {
        const empty = list.querySelector(":scope > li.empty");
        const hasData = [...list.children].some(p => p !== empty);
        if (hasData) {
            empty?.remove();
            return;
        }
        if (empty)
            return;
        const langs = await this.getLangsAsync("DataEmpty");
        if (list.querySelector(":scope > li.empty") || list.children.length > 0)
            return;
        list.insertAdjacentHTML("beforeend", `<li class="empty"><i class="fi fiAnimalDog"></i> ${langs["DataEmpty"] ?? "Empty"}</li>`);
    }
    /**
     * 목록(ul)의 끌기 가능(draggable) 항목 li 를 드래그로 순서 변경할 수 있게 연결한다. 같은 목록에 다시 호출하면 기존 연결을 대체한다.
     * 놓았을 때 순서가 달라졌으면 onChange(항목 li 목록)를 호출한다. 항목은 li[draggable="true"] 로 표시한다.
     * @param list 대상 목록
     * @param onChange 순서 변경 콜백 (변경 후 항목 순서)
     */
    bindDragSort(list, onChange) {
        let dragged = null;
        let before = [];
        const items = () => [...list.querySelectorAll(":scope > li[draggable=\"true\"]")];
        list.ondragstart = e => {
            // closest() 안의 :scope 는 호출 대상 자신을 가리키므로 쓰지 않고, 이 목록의 직계 항목인지 따로 확인한다
            const li = e.target.closest?.("li[draggable=\"true\"]");
            dragged = li?.parentElement === list ? li : null;
            if (!dragged)
                return;
            before = items();
            e.dataTransfer.effectAllowed = "move";
            dragged.style.opacity = "0.4";
        };
        list.ondragover = e => {
            if (!dragged)
                return;
            e.preventDefault();
            const over = e.target.closest?.("li[draggable=\"true\"]");
            if (!over || over === dragged || over.parentElement !== list)
                return;
            const rect = over.getBoundingClientRect();
            list.insertBefore(dragged, e.clientY < rect.top + rect.height / 2 ? over : over.nextSibling);
        };
        list.ondrop = e => e.preventDefault();
        list.ondragend = () => {
            if (!dragged)
                return;
            dragged.style.opacity = "";
            dragged = null;
            const now = items();
            if (now.some((li, i) => li !== before[i]))
                onChange?.(now);
        };
    }
    /**
     * 키-값 속성 상자(.keyValueBox)의 일반 속성 수집 (시스템 속성 제외)
     * @param box 속성 상자 루트
     * @returns 일반 속성 (Key: 속성키, Value: 값)
     */
    getProps(box) {
        return Object.fromEntries([...box.querySelectorAll(":scope > ul > li")]
            .filter(li => li.querySelector(":scope > input"))
            .map(li => [li.querySelector(":scope > header").textContent ?? "", li.querySelector(":scope > input").value]));
    }
    /**
     * 목록 화면 공용 페이지 크기를 세션 스토리지에서 읽어 표시 개수 선택(select)에 반영하고 그 값을 반환한다.
     * 저장된 값이 선택 목록에 없으면 현재 선택값을 쓴다. (변경 시 저장은 body change 위임이 처리)
     * @param select 표시 개수 선택 select[name="pageSize"]
     * @returns 적용된 페이지 크기
     */
    applyPageSize(select) {
        const saved = String(this.getPageSize(0));
        if ([...select.options].some(p => p.value === saved))
            select.value = saved;
        return Number(select.value);
    }
    /**
     * 목록 화면 공용 페이지 크기(세션 스토리지 listPageSize)를 반환한다. 저장된 값이 없으면 fallback.
     * @param fallback 기본 페이지 크기
     */
    getPageSize(fallback = 10) {
        let saved = 0;
        try {
            saved = Number(sessionStorage.getItem("listPageSize"));
        }
        catch { }
        return saved > 0 ? saved : fallback;
    }
    /**
     * HTML 템플릿 태그: 템플릿 안의 ${값} 은 자동으로 이스케이프되고(글자로만 출력), 다른 html`` 결과나 배열 안의 html`` 은 그대로 들어간다.
     * 목록 한 줄처럼 HTML 을 눈으로 보며 고칠 수 있게 쓰는 용도 — 값에 서버가 준 글자(제목, 이름 등)를 넣어도 HTML 로 해석되지 않는다.
     * 예) common.html`<li><a href="/view/${id}">${title}</a>${isNew ? common.html`<i>NEW</i>` : ''}</li>`
     * @returns 이스케이프가 끝난 HTML (문자열처럼 이어 붙일 수 있음)
     */
    html = (strings, ...values) => {
        const esc = (v) => Array.isArray(v) ? v.map(esc).join("")
            : v instanceof SafeHtml ? v.html
                : String(v ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
        return new SafeHtml(strings.reduce((out, s, i) => out + s + (i < values.length ? esc(values[i]) : ""), ""));
    };
    /**
     * 목록(ul 등)에 데이터를 바인딩한다: 항목마다 rowOf 가 돌려준 HTML(보통 common.html`<li>…</li>`)을 이어 붙여 채우고, 항목이 없으면 empty 를 채운다.
     * @param list 채울 요소 (기존 내용은 지워짐)
     * @param items 데이터 목록
     * @param rowOf 항목 하나의 HTML 을 만드는 함수 (한 줄 템플릿)
     * @param empty 항목이 없을 때 채울 HTML (생략하면 비움)
     */
    bindList = (list, items, rowOf, empty = "") => {
        list.innerHTML = items.length ? items.map((item, index) => String(rowOf(item, index) ?? "")).join("") : String(empty ?? "");
    };
    /**
     * 여러 줄 글 입력 모달 표시 (반려 사유 같은 짧은 의견 입력, modals/textInput).
     * @param content 입력 위 안내 문구 (글자 그대로 출력)
     * @param options title(제목), placeholder, value(처음 값), maxLength(기본 500), required(비면 확인 안 됨), danger(경고 색), icon(제목 아이콘 클래스)
     * @returns 확인 시 입력한 글(앞뒤 공백 제거), 취소 시 null
     */
    textInput = async (content, options = {}) => {
        const result = await this.modal.append("textInput", { content, ...options });
        return typeof result === "string" ? result : null;
    };
    /**
     * 추가 확인 경고창 표시 (삭제 등 되돌릴 수 없는 작업용, modals/checkConfirm). 문구는 "@키" 로 다국어 리소스를 쓸 수 있다.
     * @param content 본문 (HTML 허용)
     * @param items 체크 옵션 (Key: 반환 키, Value: 표시 문구 또는 { text, checked: 기본 체크 })
     * @param checked 처음부터 체크할 옵션 키 목록 (옵션별 checked 와 함께 사용 가능)
     * @param title 제목 (생략 시 "@Confirm")
     * @returns 확인 시 체크된 옵션 키 배열, 취소 시 null
     */
    checkConfirm = async (content, items = {}, checked = [], title) => {
        const result = await this.modal.append("checkConfirm", { title, content, items, checked });
        return Array.isArray(result) ? result : null;
    };
    /**
     * 다국어 사전 비동기 조회 (세션 캐시 우선)
     * @param keys 쉼표로 구분된 다국어 키 목록 (예: "Cancel, Confirm")
     * @returns 다국어 키-값 맵
     */
    async getLangsAsync(keys) {
        const keyList = keys?.split(",").map(p => p.trim()).filter(p => p !== "") || [];
        const resultDict = {};
        for (const k of keyList) {
            const cached = sessionStorage.getItem("lang@" + k.toLowerCase());
            if (cached && cached.trim() !== "")
                resultDict[k] = cached.trim();
        }
        const missingKeys = keyList.filter(k => !(k in resultDict));
        if (missingKeys.length === 0)
            return resultDict;
        const path = `/api/system/langs?keys=${encodeURIComponent(missingKeys.join(","))}`;
        const res = await this.getSend(path, { useLoading: false });
        const fetchedData = res?.data;
        if (fetchedData) {
            missingKeys.forEach(k => {
                const val = fetchedData[k] || k;
                resultDict[k] = val;
                sessionStorage.setItem("lang@" + k.toLowerCase(), val);
            });
        }
        else {
            missingKeys.forEach(k => resultDict[k] = k);
        }
        return resultDict;
    }
    /**
     * Ajax 원시 네트워크 요청 처리 (표준 JSON 파싱 및 오류 핸들링 내장)
     * @param url 요청 경로
     * @param options Fetch 및 로딩 옵션
     * @returns IResult 응답 객체 또는 null
     */
    async netSend(url, options = {}) {
        const useLoading = options.useLoading !== false;
        const loadBox = useLoading ? this.showLoading() : null;
        const headers = { ...options.headers };
        try {
            const response = await fetch(url, {
                credentials: "include",
                ...options,
                headers,
            });
            if (!response.ok) {
                const errorText = await response.text();
                let parsedResult = null;
                try {
                    parsedResult = JSON.parse(errorText);
                }
                catch { }
                // 세션 만료 / 미인증(401) 시 로그인 페이지로 자동 리다이렉트
                if (response.status === 401) {
                    const msg = parsedResult?.message || (await this.getLangsAsync("AuthRequired"))["AuthRequired"];
                    this.alert(msg, "Error");
                    const returnUrl = encodeURIComponent(location.pathname + location.search);
                    location.href = `${this.loginUrl}?ReturnUrl=${returnUrl}`;
                    return parsedResult;
                }
                if (parsedResult?.message) {
                    this.alert(parsedResult.message, "Error");
                    return parsedResult;
                }
                throw new Error(`HTTP ${response.status}: ${errorText || response.statusText}`);
            }
            const resData = (await response.json());
            if (resData.success) {
                if (resData.message && options.useInfo !== false)
                    this.info(resData.message, "Complete");
                return resData;
            }
            else if (resData.message) {
                this.alert(resData.message, "Alert");
            }
            return resData;
        }
        catch (error) {
            console.error(`[Ajax Network Error] ${options.method || "GET"} ${url}:`, error);
            this.alert("Network connection error. Please try again.", "Error");
            return null;
        }
        finally {
            loadBox?.close();
        }
    }
    /**
     * GET 요청 처리
     * @param url 호출 경로
     * @param options 요청 옵션
     */
    async getSend(url, options = {}) {
        return this.netSend(url, { method: "GET", ...options });
    }
    /**
     * POST 요청 처리
     * @param url 호출 경로
     * @param body 전송 본문 객체 또는 FormData
     * @param options 요청 옵션
     */
    async postSend(url, body, options = {}) {
        const isFormData = body instanceof FormData;
        const opt = isFormData
            ? { body }
            : { headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined };
        return this.netSend(url, { method: "POST", ...opt, ...options });
    }
    /**
     * PUT 요청 처리
     * @param url 호출 경로
     * @param body 전송 본문 객체 또는 FormData
     * @param options 요청 옵션
     */
    async putSend(url, body, options = {}) {
        const isFormData = body instanceof FormData;
        const opt = isFormData
            ? { body }
            : { headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined };
        return this.netSend(url, { method: "PUT", ...opt, ...options });
    }
    /**
     * PATCH 요청 처리
     * @param url 호출 경로
     * @param body 전송 본문 객체 또는 FormData
     * @param options 요청 옵션
     */
    async patchSend(url, body, options = {}) {
        const isFormData = body instanceof FormData;
        const opt = isFormData
            ? { body }
            : { headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined };
        return this.netSend(url, { method: "PATCH", ...opt, ...options });
    }
    /**
     * DELETE 요청 처리
     * @param url 호출 경로
     * @param options 요청 옵션
     */
    async delSend(url, options = {}) {
        return this.netSend(url, { method: "DELETE", ...options });
    }
    /**
     * 파일 업로드 전송 처리
     * @param url 호출 경로
     * @param formData 전송할 FormData
     * @param options 요청 옵션
     */
    async fileSend(url, formData, options = {}) {
        return this.netSend(url, { method: "POST", body: formData, ...options });
    }
    /**
     * 파일 용량(byte)을 읽기 쉬운 글자로 바꾼다 (예: 1536 → "1.5 KB")
     * @param bytes 용량 (byte)
     */
    fileSizeText(bytes) {
        if (!(bytes > 0))
            return "0 B";
        const i = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), 4);
        return parseFloat((bytes / Math.pow(1024, i)).toFixed(i === 0 ? 0 : 1)) + " " + ["B", "KB", "MB", "GB", "TB"][i];
    }
    /**
     * 파일 업로드 전송 + 진행률 (XMLHttpRequest): 로딩 마스크나 오류 알림 없이 결과만 돌려준다 — 파일마다 진행 상태를 항목에 직접 보여 주는 화면용.
     * @param url 호출 경로
     * @param formData 전송할 FormData
     * @param onProgress 전송 진행률(0~100) 콜백
     * @param signal 전송 취소 신호 (취소하면 null 을 돌려줌)
     * @returns 서버 응답(JSON, 실패 상태여도 JSON 이면 그대로), 네트워크 오류·취소는 null
     */
    upload(url, formData, onProgress, signal) {
        return new Promise(resolve => {
            const xhr = new XMLHttpRequest();
            xhr.open("POST", url);
            xhr.withCredentials = true;
            xhr.upload.onprogress = e => { if (e.lengthComputable)
                onProgress?.(Math.round(e.loaded / e.total * 100)); };
            xhr.onload = () => {
                try {
                    resolve(JSON.parse(xhr.responseText));
                }
                catch {
                    resolve(null);
                }
            };
            xhr.onerror = xhr.onabort = () => resolve(null);
            signal?.addEventListener("abort", () => xhr.abort(), { once: true });
            xhr.send(formData);
        });
    }
    /**
     * 첨부 파일 하나 올리기: 조각 크기(5MB) 이하는 {@link upload} 한 번으로, 큰 파일(최대 2GB)은 조각 업로드(`{url}/chunk/start` → `{url}/chunk` 반복 → `{url}/chunk/complete`)로 보낸다.
     * 조각은 차례로 보내고, 끊기면 서버가 알려 준 받은 크기부터 다시 보낸다(최대 5번 재시도). 취소하면 서버의 조각도 지운다(`{url}/chunk/cancel`).
     * @param url 업로드 경로 (예: /api/content/file)
     * @param file 올릴 파일
     * @param fields 함께 보낼 폼 값 (규칙 번호, 올릴 곳 등 — 시작·완료 요청에 같이 보냄)
     * @param onProgress 전송 진행률(0~100) 콜백
     * @param signal 전송 취소 신호 (취소하면 null)
     * @returns 서버 응답 (작은 파일 업로드·완료 요청의 응답, 실패면 그 실패 응답), 네트워크 오류·취소는 null
     */
    async uploadFile(url, file, fields, onProgress, signal) {
        const smallSize = 5 * 1024 * 1024;
        if (file.size <= smallSize)
            return this.upload(url, this.makeFormData({ ...fields, file }), onProgress, signal);
        const start = await this.upload(`${url}/chunk/start`, this.makeFormData({ ...fields, fileName: file.name, fileSize: file.size }), undefined, signal);
        if (!start?.success)
            return start;
        const { uploadId, chunkSize } = start.data;
        const cancel = () => { this.upload(`${url}/chunk/cancel`, this.makeFormData({ uploadId })); return null; };
        let received = 0, retry = 0;
        while (received < file.size) {
            const part = file.slice(received, received + chunkSize);
            const res = await this.upload(`${url}/chunk`, this.makeFormData({ uploadId, offset: received, chunk: part }), percent => onProgress?.(Math.min(99, Math.floor((received + part.size * percent / 100) / file.size * 100))), signal);
            if (signal?.aborted)
                return cancel();
            if (res?.success) {
                received = res.data.received;
                retry = 0;
                continue;
            }
            if (res) { // 서버가 거부 (업로드 없음·용량 초과 등): 더 보내지 않음
                cancel();
                return res;
            }
            if (++retry > 5)
                return cancel();
            await new Promise(resolve => setTimeout(resolve, retry * 1000)); // 네트워크 오류: 잠시 뒤 받은 위치부터 다시 (서버가 위치를 바로잡아 줌)
        }
        onProgress?.(99);
        const done = await this.upload(`${url}/chunk/complete`, this.makeFormData({ ...fields, uploadId }), undefined, signal);
        if (signal?.aborted)
            return cancel();
        return done;
    }
    /**
     * 객체를 FormData(개별 폼 필드)로 변환. 서버는 모델 클래스 없이 개별 파라미터로 받는다.
     * 배열은 같은 이름의 반복 필드, 파일(Blob)은 그대로, 그 밖의 객체(사전)는 JSON 문자열 필드로 담고 null/undefined 는 제외한다.
     * @param params 변환 대상 맵 객체
     */
    makeFormData(params) {
        const fd = new FormData();
        const append = (k, v) => fd.append(k, v instanceof Blob ? v : typeof v === "object" ? JSON.stringify(v) : String(v));
        Object.keys(params).forEach(k => {
            const v = params[k];
            if (v === undefined || v === null)
                return;
            if (Array.isArray(v))
                v.forEach(p => append(k, p));
            else
                append(k, v);
        });
        return fd;
    }
    /**
     * 대상 엘리먼트의 열기 처리 및 상태 저장
     * @param node 대상 엘리먼트
     * @param saveName 상태 저장을 위한 명칭 (sessionStorage 키)
     */
    openedToggle(node, saveName) {
        if (!node)
            return;
        if (node.classList.contains("opened")) {
            node.classList.remove("opened");
        }
        else {
            node.classList.add("opened");
        }
        if (saveName) {
            sessionStorage.setItem(saveName, node.classList.contains("opened") ? "opened" : "");
        }
    }
    /**
     * 로딩 스피너 DOM 생성
     * @param msg 안내 메시지 (선택)
     */
    makeLoadingBox(msg) {
        const wrap = document.createElement("div");
        wrap.className = "loadingWrap";
        wrap.innerHTML = `
            <svg class="loadingSvg" viewBox="0 0 80 80">
                <circle cx="40" cy="40" r="32" />
                <circle cx="40" cy="40" r="32" />
            </svg>
            ${msg ? `<div>${msg}</div>` : ""}
        `;
        return wrap;
    }
    /**
     * 화면 전체를 덮는 로딩 레이어 표시
     * @param msg 로딩 안내 메시지 (선택)
     * @returns 로딩 레이어 닫기 핸들러
     */
    showLoading(msg) {
        const layer = document.createElement("div");
        layer.popover = "manual";
        layer.classList.add("loading");
        layer.appendChild(this.makeLoadingBox(msg));
        document.body.appendChild(layer);
        try {
            layer.showPopover();
        }
        catch {
            layer.style.display = "flex";
        }
        return {
            close: () => {
                try {
                    layer.hidePopover();
                }
                catch {
                    layer.style.display = "none";
                }
                setTimeout(() => layer.remove(), 200);
            }
        };
    }
    /**
     * 에디터 본문 HTML 을 사이트 문서에 직접 출력한다 (포럼 댓글·FAQ 답변과 같은 방식). 보안은 서버 정제(EditorFile.Sanitize: 저장할 때와 내려줄 때 두 번)가 맡고,
     * 본문 상자(.contentBody)는 CSS contain 으로 그 안의 요소가 상자 밖 화면을 덮지 못하게 한다. 본문 링크는 새 창(opener 차단)으로 열고, 에디터 본문 스타일(공급자의 content CSS·본문 클래스)을 적용한다.
     * @param target 출력 대상 요소 (기존 자식은 교체)
     * @param html 본문 HTML (서버에서 정제된 값)
     * @returns 본문 상자
     */
    renderContent(target, html) {
        const box = document.createElement("div");
        box.className = `contentBody ${this.editorProvider.content(this.editorPath).bodyClass}`;
        box.innerHTML = html;
        box.querySelectorAll("a[href]").forEach(a => { a.target = "_blank"; a.rel = "noopener noreferrer nofollow"; });
        this.loadEditorAssets(true);
        target.replaceChildren(box);
        return box;
    }
    /**
     * 에디터 UI 언어: 영어("en")가 기본이고, 현재 UI 문화권(예: "ko", "ko-KR")을 현재 공급자가 지원하면 그 언어를 쓴다. 어떤 공급자든 같은 규칙.
     */
    get editorLanguage() {
        const code = this.culture.toLowerCase();
        return [code, code.split("-")[0]].find(c => this.editorProvider.languages.includes(c)) ?? "en";
    }
    /** 현재 에디터 공급자 (editorName) */
    get editorProvider() {
        const provider = this.editorProviders.get(this.editorName);
        if (!provider)
            throw new Error(`Unknown editor provider: ${this.editorName}`);
        return provider;
    }
    /**
     * 에디터 공급자 등록 (이름이 같으면 교체). 새 에디터는 IEditorProvider 를 구현해 Site 생성자에서 등록한다.
     * @param provider 에디터 공급자
     */
    registerEditor(provider) { this.editorProviders.set(provider.name, provider); }
    /** 등록된 에디터 공급자 이름 목록 (관리자 "사이트 정보" 의 주 에디터 선택지) */
    get editorNames() { return [...this.editorProviders.keys()]; }
    /**
     * 현재 에디터 엔진 파일(CSS/JS)을 페이지에 로드한다 (한 번만, 이미 있으면 건너뜀). makeEditor 가 호출하므로 뷰에서 파일을 직접 넣지 않는다.
     * @param stylesOnly true 이면 본문 스타일 CSS 만 로드 (에디터 없이 에디터로 만든 본문을 보여 주는 페이지용)
     */
    loadEditorAssets(stylesOnly = false) {
        const provider = this.editorProvider;
        const language = this.editorLanguage;
        const assets = provider.assets(this.editorPath, language);
        const css = stylesOnly ? provider.content(this.editorPath).css : assets.css;
        const key = `${provider.name}|${this.editorPath}|${language}|${stylesOnly}`;
        if (!this.editorLoads.has(key)) {
            this.editorLoads.set(key, (async () => {
                await Promise.all(css.map(href => loadAsset("link", href)));
                if (!stylesOnly)
                    for (const src of assets.js)
                        await loadAsset("script", src);
            })());
        }
        return this.editorLoads.get(key);
    }
    /**
     * 에디터 생성: 현재 공급자(editorName)의 엔진 파일을 로드한 뒤 textarea 를 에디터로 바꾼다.
     * 이미지는 api/system/editor/file 로 업로드해 받은 경로로 넣고(이미지 버튼, 파일 붙여넣기·끌어놓기),
     * 붙여넣거나 끌어놓은 HTML 의 다른 소유자 에디터 이미지는 서버에서 현재 소유자로 복사한 뒤 경로를 바꾼다. 미디어 기능은 제외한다.
     * 소유자/암호는 업로드할 때마다 option 에서 다시 읽는다(getter 사용 가능).
     * @param target 에디터로 바꿀 textarea
     * @param option 소유자 형식/식별키, 비인증 등록 암호, 높이, 파일 처리 결과 콜백, 사이트 CSS·치환 태그·걸러내기 해제, 엔진 고유 옵션
     * @returns 에디터 인스턴스 (본문: editor.getHtml())
     */
    async makeEditor(target, option) {
        await this.loadEditorAssets();
        const fill = (option.height ?? "100%") === "100%";
        if (fill)
            target.parentElement?.classList.add("editorFill");
        return this.editorProvider.create(target, option, this.editorHost, fill);
    }
    /** 공급자에게 빌려주는 공용 기능 (이미지 업로드/복사) */
    get editorHost() {
        return {
            language: this.editorLanguage,
            uploadImages: (files, option) => this.uploadEditorImages(files, option),
            copyImages: (html, option) => this.copyEditorImages(html, option),
        };
    }
    /** 에디터 이미지 업로드: 서버에 올리고 [{ url, name, size }] 를 돌려준다 (실패하면 null) */
    async uploadEditorImages(files, option) {
        const form = this.makeFormData({ file: files, password: option.password || null });
        const query = new URLSearchParams({ ownerType: option.ownerType, ownerKey: option.ownerKey });
        const res = await this.postSend(`/api/system/editor/file?${query}`, form);
        if (!res?.success)
            return null;
        option.onFiles?.("upload", res.data);
        return res.data;
    }
    /** 붙여넣기/끌어놓기 HTML 의 에디터 이미지를 현재 소유자로 서버 복사하고 경로를 바꾼 HTML 을 돌려준다 (이미지가 없으면 그대로) */
    async copyEditorImages(html, option) {
        const pattern = /(?:https?:\/\/[^\/"'\s]+)?\/api\/system\/editor\/file\/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\/[0-9a-f]{5}[0-9a-f]*/gi;
        const ids = [...new Set([...html.matchAll(pattern)].map(m => m[1].toLowerCase()))];
        if (!ids.length)
            return;
        const res = await this.postSend("/api/system/editor/file/copy", this.makeFormData({
            ownerType: option.ownerType, ownerKey: option.ownerKey, ids, password: option.password || null
        }), { useLoading: false });
        if (!res?.success)
            return false;
        option.onFiles?.("copy", res.data);
        return html.replace(pattern, (url, id) => res.data[id] ?? res.data[id.toLowerCase()] ?? url);
    }
    /**
     * 트리 컨트롤 생성 팩토리
     * @param option 트리 옵션
     * @returns TreeBox 인스턴스
     */
    makeTree(option) { return new TreeBox(option); }
    /**
     * 기준 요소 옆에 확장 메뉴를 띄운다 (목록 행의 ⋮ 버튼 등, 트리 확장 메뉴와 같은 모양)
     * @param anchorEl 기준 요소
     * @param items 메뉴 항목 ({ text, icon, danger, divider, action })
     */
    popMenu(anchorEl, items, align = "left", follow = false) { showPopMenu(anchorEl, items, align, follow); }
    /**
     * 서버가 준 표준시(UTC) 시각 문자열을 보는 사람의 시간대 날짜/시각 글자로 바꾼다. 서버·DB 의 시각은 항상 UTC 이고 화면에 보일 때만 이 함수로 바꾼다.
     * 시간대 표시(Z, +09:00)가 없는 문자열은 UTC 로 본다.
     * @param value UTC 시각 (ISO 8601, 없거나 잘못되면 빈 문자열)
     * @param withTime true 면 "yyyy-MM-dd HH:mm", 아니면 "yyyy-MM-dd"
     */
    localTime(value, withTime = false) {
        if (!value)
            return "";
        const d = new Date(/(?:Z|[+-]\d\d:\d\d)$/i.test(value) ? value : value + "Z");
        if (isNaN(d.getTime()))
            return "";
        const p = (n) => String(n).padStart(2, "0");
        const date = `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
        return withTime ? `${date} ${p(d.getHours())}:${p(d.getMinutes())}` : date;
    }
    /**
     * 글·댓글을 신고한다: 큰 입력 모달로 신고 내용을 받아 api/content/report 로 접수한다 (접수·오류 안내는 공용 응답 처리가 보여 줌).
     * 신고 대상 글의 식별 정보와 신고 내용, 신고자는 서버가 신고 테이블에 기록한다. 로그인하지 않았으면 서버가 거부한다.
     * @param target 신고 대상: ruleNo(게시판), contentNo(글 또는 포럼 댓글의 글번호), 기본 댓글이면 commentId
     * @returns 접수되었으면 true, 취소·실패면 false
     */
    async reportContent(target) {
        const langs = await this.getLangsAsync("Report, ReportMessage");
        const message = await this.textInput("", { title: langs["Report"], placeholder: langs["ReportMessage"], required: true, rows: 10, maxLength: 2000, icon: "fiFlag", wide: true, danger: true });
        if (message === null)
            return false;
        const res = await this.postSend("/api/content/report", this.makeFormData({ ...target, message }));
        return !!res?.success;
    }
    /**
     * 서버가 준 표준시(UTC) 시각을 짧은 상대 시간 글자로 바꾼다: 문서 언어의 Intl.RelativeTimeFormat(짧은 형식) — 예: 한국어 "지금 / 3분 전 / 2시간 전 / 5일 전", 영어 "now / 3m ago / 2h ago / 5d ago".
     * 기본 30일, <paramref name="maxDays"/> 일이 넘으면 날짜(yyyy-MM-dd). 미래 시각은 "지금"/"now" 로 본다.
     * @param value UTC 시각 (ISO 8601, 없거나 잘못되면 빈 문자열)
     * @param maxDays 상대 시간으로 보여 주는 최대 일수 (기본 30, 이 일수까지는 "N일 전")
     */
    ago(value, maxDays = 30) {
        if (!value)
            return "";
        const d = new Date(/(?:Z|[+-]\d\d:\d\d)$/i.test(value) ? value : value + "Z");
        if (isNaN(d.getTime()))
            return "";
        const lang = document.documentElement.lang || navigator.language;
        const rtf = new Intl.RelativeTimeFormat(lang, { style: "narrow" });
        const min = Math.floor((Date.now() - d.getTime()) / 60000);
        if (min < 1)
            return new Intl.RelativeTimeFormat(lang, { numeric: "auto" }).format(0, "second");
        if (min < 60)
            return rtf.format(-min, "minute");
        if (min < 1440)
            return rtf.format(-Math.floor(min / 60), "hour");
        if (min < (maxDays + 1) * 1440)
            return rtf.format(-Math.floor(min / 1440), "day");
        return this.localTime(value);
    }
    /**
     * 기간(시작~종료 일시) 설정 모달을 연다. 비워 두면 그쪽은 제한 없음이며, 일시는 사용자의 로컬 시간대로 보여 주고 UTC 로 돌려준다.
     * @param start 시작 일시 (ISO 8601 UTC, 없으면 제한 없음)
     * @param end 종료 일시 (ISO 8601 UTC, 없으면 제한 없음)
     * @param title 대화상자 제목 문자열(생략 시 "발행 기간"), 또는 문구 교체 옵션 { title, icon, startLabel, endLabel, info } (info 를 ""로 주면 하단 안내 숨김)
     * @returns 확인 시 { start, end } (ISO 8601 UTC 문자열 또는 null), 취소/Esc 시 null
     */
    periodEdit(start, end, title) {
        return this.modal.append("periodEdit", { start, end, ...(typeof title === "string" ? { title } : title) });
    }
}
/**
 * 전역 window.common 인스턴스를 동적으로 참조하는 프록시 객체.
 * 페이지 상에서 new Site(config)로 초기화된 인스턴스를 투명하게 대리 호출합니다.
 */
const commonHandler = {
    get(_, prop) {
        const inst = window["common"];
        if (!inst)
            return undefined;
        const val = inst[prop];
        return typeof val === "function" ? val.bind(inst) : val;
    },
    set(_, prop, value) {
        const inst = window["common"];
        if (inst)
            inst[prop] = value;
        return true;
    }
};
export const common = new Proxy({}, commonHandler);
export default common;
/** loadAsset 이 이미 시작한 파일 (경로 → 로드 완료 Promise) */
const loadedAssets = new Map();
/**
 * CSS(link)/JS(script) 파일 한 개를 페이지에 로드하고 끝나면 resolve 한다. 같은 경로는 한 번만 넣고, 페이지 HTML 에 이미 있는 태그는 로드된 것으로 본다.
 * @param kind 파일 종류
 * @param url 파일 경로
 */
function loadAsset(kind, url) {
    const attr = kind === "link" ? "href" : "src";
    if (!loadedAssets.has(url)) {
        loadedAssets.set(url, new Promise((resolve, reject) => {
            if (document.querySelector(`${kind}[${attr}="${url}"]`))
                return resolve();
            const el = document.createElement(kind);
            if (kind === "link")
                el.rel = "stylesheet";
            el[attr] = url;
            el.onload = () => resolve();
            el.onerror = () => { loadedAssets.delete(url); reject(new Error(`Failed to load ${url}`)); };
            document.head.appendChild(el);
        }));
    }
    return loadedAssets.get(url);
}
/**
 * 드롭다운 요소 열기/닫기 토글
 * @param wrap 드롭다운 루트 요소
 */
function toggleDropDown(wrap) {
    if (wrap.classList.contains("open")) {
        wrap.classList.remove("open");
        if (wrap.classList.contains("active"))
            wrap.classList.remove("active");
    }
    else {
        wrap.classList.add("open");
        if (!wrap.classList.contains("active"))
            wrap.classList.add("active");
    }
}
/**
 * 칩 목록 상자의 칩 생성 (기본 지정 라디오 + 표시 텍스트 + 삭제 버튼)
 * @param box 칩 목록 상자 루트 (라디오 그룹명: data-name, 기본 "chipDefault")
 * @param key 항목 키
 * @param text 표시 텍스트
 * @param checked 기본 지정 여부
 * @returns 칩 항목
 */
function makeChip(box, key, text, checked) {
    const li = document.createElement("li");
    li.dataset.key = key;
    li.innerHTML = `<label class="radio"><input type="radio" name="${box.dataset.name || "chipDefault"}"${checked ? " checked" : ""} /><span></span></label><button type="button" class="sm danger"><i class="fi fiDismiss"></i></button>`;
    li.querySelector("span").textContent = text;
    return li;
}
/**
 * 칩 목록 동기화: 빈 데이터 항목 처리 + 기본 지정 항목이 없으면 첫 항목을 자동 지정 (바인딩/추가/삭제 공통)
 * @param list 칩 목록 ul
 */
function syncChips(list) {
    const radios = [...list.querySelectorAll(":scope > li[data-key] input[type=\"radio\"]")];
    if (radios.length > 0 && !radios.some(p => p.checked))
        radios[0].checked = true;
    common.syncEmpty(list);
}
/**
 * 칩 목록 상자의 추가 버튼 처리: data-modal 에 지정된 선택 모달을 연다.
 * 기본은 이미 있는 항목을 선택 불가로 열어 선택 결과를 칩으로 추가하고, data-sync 가 있으면 현재 항목을 체크된 상태로 열어
 * 선택 결과로 동기화(해제된 칩 제거, 새 칩 추가, 남은 칩의 기본 지정 유지)한다. 기본 지정이 사라지면 첫 항목을 자동 지정한다.
 * @param box 칩 목록 상자 루트
 */
async function addChips(box) {
    if (!box.dataset.modal)
        return;
    const list = box.querySelector(":scope > ul");
    const current = new Map([...list.querySelectorAll(":scope > li[data-key]")].map(li => [li.dataset.key, li]));
    const sync = box.dataset.sync !== undefined;
    // data-sync: 현재 항목을 체크된 상태로 열어 최종 선택 결과로 동기화(숨김 항목이 목록에서 빠져 해제되지 않도록 숨김 포함)
    // 기본: 현재 항목은 선택 불가로 열어 추가만
    const args = sync
        ? { includeHidden: true, selectedKeys: [...current.keys()], selected: Object.fromEntries([...current].map(([key, li]) => [key, li.querySelector("span").textContent])) }
        : { disableKeys: [...current.keys()] };
    const result = await common.modal.append(box.dataset.modal, args);
    if (!result || typeof result !== "object")
        return;
    const picked = result;
    if (sync)
        current.forEach((li, key) => { if (!(key in picked))
            li.remove(); });
    Object.entries(picked).forEach(([key, text]) => { if (!current.has(key))
        list.appendChild(makeChip(box, key, text, false)); });
    syncChips(list);
}
/**
 * 태그 목록의 태그 항목 생성 (표시 텍스트 + 쓰기 모드일 때 삭제 버튼)
 * @param text 태그 텍스트
 * @param writable 쓰기 모드 여부 (삭제 버튼 포함)
 * @returns 태그 항목
 */
function makeTag(text, writable) {
    const li = document.createElement("li");
    li.innerHTML = `<span></span>${writable ? `<button type="button"><i class="fi fiDismiss"></i></button>` : ""}`;
    li.querySelector("span").textContent = text;
    return li;
}
/**
 * 태그 목록 쓰기 입력항의 값을 태그로 추가: 입력항 li 앞에 넣고 입력을 비운다. (빈 값 무시, 대소문자 무시 중복은 추가하지 않음)
 * 추가되면 ul 에 "tag" 이벤트(detail: { text, add: true }) 발생
 * @param input ul.tags 자식 li 안의 입력항
 */
function addTag(input) {
    const text = input.value.trim();
    input.value = "";
    if (!text)
        return;
    const list = input.closest("ul.tags");
    if (common.getTags(list).some(p => p.toLowerCase() === text.toLowerCase()))
        return;
    list.insertBefore(makeTag(text, true), input.parentElement);
    list.dispatchEvent(new CustomEvent("tag", { detail: { text, add: true } }));
}
/**
 * 키-값 속성 상자의 속성 행 생성 (시스템 속성은 값 span, 일반 속성은 값 입력 + 삭제 버튼)
 * @param key 속성키
 * @param value 속성값
 * @param system 시스템 속성 여부
 * @returns 속성 행
 */
function makePropRow(key, value, system) {
    const li = document.createElement("li");
    const header = document.createElement("header");
    header.textContent = key;
    header.title = key;
    li.appendChild(header);
    if (system) {
        const span = document.createElement("span");
        span.textContent = value;
        span.title = value;
        li.appendChild(span);
    }
    else {
        const input = document.createElement("input");
        input.type = "text";
        input.value = value;
        input.title = value;
        input.addEventListener("input", () => input.title = input.value);
        li.appendChild(input);
        li.insertAdjacentHTML("beforeend", `<button type="button" class="sm danger"><i class="fi fiDelete"></i></button>`);
    }
    return li;
}
/**
 * 키-값 속성 상자 하단 입력값으로 새 속성 행 추가 (빈 키/"@" 접두 키/중복 키 거부)
 * @param box 속성 상자 루트
 */
async function addPropRow(box) {
    const [keyInput, valueInput] = [...box.querySelectorAll(":scope > footer > input")];
    const key = keyInput.value.trim();
    if (!key) {
        keyInput.focus();
        return;
    }
    const langs = await common.getLangsAsync("PropKeyDup, PropKeyInvalid");
    if (key.startsWith("@")) {
        common.alert(langs["PropKeyInvalid"]);
        return;
    }
    const keys = [...box.querySelectorAll(":scope > ul > li > header")].map(p => (p.textContent ?? "").toLowerCase());
    if (keys.includes(key.toLowerCase())) {
        common.alert(langs["PropKeyDup"]);
        return;
    }
    const list = box.querySelector(":scope > ul");
    list.appendChild(makePropRow(key, valueInput.value, false));
    common.syncEmpty(list);
    keyInput.value = valueInput.value = "";
    keyInput.focus();
}
/**
 * 모달 스택 정리 및 중첩 상태 속성 갱신 (닫히거나 DOM 에서 빠진 모달은 제거)
 * - data-first: 첫 모달 (배경 어둡게 + 블러)
 * - data-under: 최상단 이전 모달 (자체 블러)
 * @param stack 열린 순서대로의 dialog 목록 (제자리 갱신)
 */
function syncModalStack(stack) {
    stack.filter(p => !p.open || !p.isConnected).forEach(p => {
        ValidResult.clearErrors(p); // 닫힌 모달의 유효성 오류 표시 해제
        p.removeAttribute("data-first");
        p.removeAttribute("data-under");
        stack.splice(stack.indexOf(p), 1);
    });
    stack.forEach((p, i) => {
        p.toggleAttribute("data-first", i === 0);
        p.toggleAttribute("data-under", i < stack.length - 1);
    });
}
document.addEventListener("DOMContentLoaded", () => {
    // 전역 클릭 리스너 (드롭다운 토글/선택/외부클릭 닫기, 탭박스 전환)
    document.body.addEventListener("click", (e) => {
        const target = e.target;
        if (!target)
            return;
        const evBlock = target.closest(".eventBlock");
        if (evBlock)
            return;
        // 드롭다운 토글
        const dropDownOpen = target.closest(".dropDown > header, .dropMenu > i.fi");
        if (dropDownOpen && dropDownOpen.parentElement)
            toggleDropDown(dropDownOpen.parentElement);
        // 드롭다운 아이템 선택 (단일 선택 드롭다운만 헤더 텍스트 자동 반영)
        const dropDownItem = target.closest(".dropDown:not(.noSelect):not(.checker) > ul > li");
        if (dropDownItem) {
            const dropDown = dropDownItem.closest(".dropDown");
            if (dropDown) {
                const value = dropDownItem.dataset.val;
                if (value)
                    dropDown.dataset.val = value;
                const header = dropDown.querySelector("header");
                if (header)
                    header.innerText = dropDownItem.innerText;
                dropDown.dispatchEvent(new CustomEvent("change", {
                    detail: {
                        value: value,
                        text: dropDownItem.innerText
                    }
                }));
            }
        }
        // 다국어 입력항 바로 옆 버튼 → 공용 다국어 입력 모달(제목: 입력항 data-title, "@"로 시작하면 다국어 키), 적용 시 입력항 JSON/표시값 갱신
        const langBtn = target.closest("input[data-lang] + button");
        if (langBtn) {
            const input = langBtn.previousElementSibling;
            common.langInput(common.getLang(input), input.dataset.title).then(map => { if (map)
                common.setLang(input, map); });
        }
        // 키-값 속성 상자: 행 삭제 버튼 / 하단 추가 버튼
        const propDel = target.closest(".keyValueBox > ul > li > button");
        if (propDel) {
            const list = propDel.closest("ul");
            propDel.parentElement.remove();
            common.syncEmpty(list);
        }
        const propAdd = target.closest(".keyValueBox > footer > button");
        if (propAdd)
            addPropRow(propAdd.closest(".keyValueBox"));
        // 칩 목록 상자: 칩 삭제 버튼 / 하단 추가 버튼(선택 모달)
        const chipDel = target.closest(".chipBox > ul > li > button");
        if (chipDel) {
            const list = chipDel.closest("ul");
            chipDel.parentElement.remove();
            syncChips(list);
        }
        const chipAdd = target.closest(".chipBox > footer > button");
        if (chipAdd)
            addChips(chipAdd.closest(".chipBox"));
        // 태그 목록: 태그 삭제 버튼 (ul 에 "tag" 이벤트 detail: { text, add: false }) / 빈 영역 클릭 시 쓰기 입력항 포커스
        const tagDel = target.closest("ul.tags > li:not(:has(> input)) > button");
        if (tagDel) {
            const li = tagDel.parentElement;
            const list = li.parentElement;
            const text = li.querySelector(":scope > span")?.textContent ?? "";
            li.remove();
            list.dispatchEvent(new CustomEvent("tag", { detail: { text, add: false } }));
        }
        if (target.matches("ul.tags"))
            target.querySelector(":scope > li > input")?.focus();
        // 페이지 이동: 활성/비활성 항목 제외하고 ul 에 "page" 이벤트 발생
        const pageItem = target.closest("ul.pager > li[data-page]:not(.active):not(.disabled)");
        if (pageItem)
            pageItem.parentElement.dispatchEvent(new CustomEvent("page", { detail: Number(pageItem.dataset.page) }));
        // 탭박스 탭 전환
        const tabHeader = target.closest(".tabBox > header > *");
        if (tabHeader) {
            const targetSelector = tabHeader.dataset["for"];
            const tabBox = tabHeader.closest(".tabBox");
            if (tabBox && targetSelector) {
                tabBox.querySelectorAll(":scope > header > *.active, :scope > section > *.active")
                    .forEach(el => el.classList.remove("active"));
                const section = tabBox.querySelector(targetSelector);
                if (section)
                    section.classList.add("active");
                tabHeader.classList.add("active");
            }
        }
        // 외부 클릭 시 .open 제거 (열기 직후 active가 부여된 요소는 제외하고 닫음). 확장 메뉴(.treeMenu, PopMenu)도 같은 방식으로 닫힌다.
        document.querySelectorAll(".open").forEach((node) => {
            if (node.classList.contains("active")) {
                node.classList.remove("active");
            }
            else {
                node.classList.remove("open");
            }
        });
    });
    // 열린 모달(dialog) 스택 감시: 페이지 내장/ModalManager 동적 모달 모두 open 속성 변화로 추적
    const modalStack = [];
    new MutationObserver(records => {
        records.forEach(r => {
            const diag = r.target;
            if (diag.tagName === "DIALOG" && diag.open && !modalStack.includes(diag))
                modalStack.push(diag);
        });
        syncModalStack(modalStack);
    }).observe(document.body, { subtree: true, attributes: true, attributeFilter: ["open"] });
    // 날짜·시간 컨트롤(.datePick/.timePick/.dateTimePick): 태그 선언만으로 쓰도록 문서에 있는 것을 초기화하고, 나중에 추가되는 것(모달 등)도 관찰해 초기화
    initPickers(document.body);
    new MutationObserver(records => records.forEach(r => r.addedNodes.forEach(n => { if (n instanceof Element)
        initPickers(n); }))).observe(document.body, { childList: true, subtree: true });
    // 시간 표시: 서버가 보관·전달하는 시각은 표준시(UTC)이고, 화면에 보일 때만 보는 사람의 시간대로 바꾼다.
    // <time data-local="datetime|date" datetime="UTC ISO 8601">UTC 글자</time> 는 문서에 있는 것을 바꾸고, 나중에 추가되는 것(모달·API 로 채운 목록 등)도 관찰해서 바꾼다. (자바스크립트가 없으면 UTC 글자가 그대로 보임)
    const formatLocalTimes = (root) => root.querySelectorAll("time[data-local]").forEach(el => {
        const d = new Date(el.dateTime);
        if (isNaN(d.getTime()) || el.dataset.shown === el.dateTime)
            return;
        const p = (n) => String(n).padStart(2, "0");
        const date = `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
        const withTime = `${date} ${p(d.getHours())}:${p(d.getMinutes())}`;
        el.textContent = el.dataset.local === "date" ? date : el.dataset.local === "ago" ? window["common"].ago(el.dateTime) : withTime; // ago: 짧은 상대 시간 (마우스를 올리면 날짜·시각)
        el.title = el.dataset.local === "ago" ? withTime : `UTC ${el.dateTime}`;
        el.dataset.shown = el.dateTime;
    });
    formatLocalTimes(document);
    new MutationObserver(records => records.forEach(r => r.addedNodes.forEach(n => { if (n instanceof Element)
        formatLocalTimes(n); }))).observe(document.body, { childList: true, subtree: true });
    // 목록 표시 개수 변경 → 목록 화면 공용 값으로 세션 스토리지에 보관 (목록 재조회는 각 화면의 change 처리)
    document.body.addEventListener("change", (e) => {
        const select = e.target?.closest?.("select[name=\"pageSize\"]");
        if (select)
            try {
                sessionStorage.setItem("listPageSize", select.value);
            }
            catch { }
    });
    // 태그 목록 쓰기 입력항: 엔터 또는 change 시 입력값을 입력항 li 앞에 태그로 추가하고 입력을 비움
    // (한글 조합 중 엔터는 조합 완료 후 처리 — 조합 중에 값을 비우면 마지막 글자가 다시 남는 문제 방지)
    document.body.addEventListener("keydown", (e) => {
        if (e.key !== "Enter")
            return;
        const input = e.target?.closest?.("ul.tags > li > input");
        if (!input)
            return;
        e.preventDefault();
        if (e.isComposing)
            input.addEventListener("compositionend", () => setTimeout(() => addTag(input)), { once: true });
        else
            addTag(input);
    });
    document.body.addEventListener("change", (e) => {
        const input = e.target?.closest?.("ul.tags > li > input");
        if (input)
            addTag(input);
    });
    // 오류 상태(error) 입력항을 다시 입력하면 오류 표시 해제
    const clearInputError = (e) => {
        const input = e.target?.closest?.("input.error, select.error, textarea.error");
        if (input)
            ValidResult.clearError(input);
    };
    document.body.addEventListener("input", clearInputError);
    document.body.addEventListener("change", clearInputError);
    // 다국어 입력항 직접 수정 → JSON 의 현재 문화권 값만 갱신 (빈 값이면 제거)
    document.body.addEventListener("input", (e) => {
        const input = e.target?.closest?.("input[data-lang]");
        if (!input)
            return;
        const map = common.getLang(input);
        const value = input.value.trim();
        if (value)
            map[common.culture] = value;
        else
            delete map[common.culture];
        input.dataset.lang = JSON.stringify(map);
    });
});
//# sourceMappingURL=site.js.map