/* ==========================================================================
   PopMenu: 기준 요소 옆에 띄우는 확장 메뉴(.treeMenu 스타일) — TreeBox 확장 메뉴와 목록 행의 ⋮ 버튼 등이 공용으로 쓴다
   ========================================================================== */
let current = null;
let currentAnchor = null;
/**
 * 기준 요소를 감싼 가장 가까운 스크롤 영역(overflow auto/scroll)을 찾는다. 없으면 null(문서 자체가 스크롤).
 * @param el 기준 요소
 */
function scrollHostOf(el) {
    for (let p = el.parentElement; p && p !== document.body && p !== document.documentElement; p = p.parentElement) {
        const cs = getComputedStyle(p);
        if (/(auto|scroll)/.test(cs.overflowY + cs.overflowX))
            return p;
    }
    return null;
}
/** 열려 있는 확장 메뉴를 닫는다. */
export function closePopMenu() {
    current?.remove();
    current = null;
    currentAnchor = null;
}
// 바깥 클릭으로 닫기는 사이트 공용 처리(site.ts 의 body 클릭: .open 요소는 닫고, 열기 직후라 active 가 함께 있으면 active 만 떼고 유지)를 따른다.
// 메뉴는 "open active" 로 열고, 연 클릭이 body 까지 오지 않거나(전파 중단) 클릭 처리 뒤 비동기로 연 경우에도 다음 클릭에 닫히도록 active 는 이번 이벤트가 끝난 뒤 뗀다.
/**
 * 기준 요소(anchorEl) 아래에 확장 메뉴를 띄운다. 이미 열린 메뉴는 닫고, 화면 밖으로 나가면 위치를 보정한다.
 * 같은 기준 요소로 연 메뉴가 열려 있으면 닫기만 한다 (기준 요소를 다시 누르면 열기/닫기 전환).
 * @param anchorEl 기준 요소 (보통 ⋮ 버튼)
 * @param items 메뉴 항목 (divider 는 구분선, action 은 클릭 시 메뉴를 닫은 뒤 호출)
 * @param align "left"(기본): 기준 요소 좌측에 맞춰 오른쪽으로 펼침, "right": 기준 요소 우측 끝에 맞춰 왼쪽으로 펼침
 * @param follow true 면 기준 요소를 감싼 스크롤 영역 안에 메뉴를 넣어 영역이 스크롤될 때 기준 요소 자리에 붙은 채 함께 움직인다(기본 false: body 에 붙어 영역에 가려지지 않음 — 트리처럼 자체 스크롤 상자 안의 버튼에는 쓰지 말 것)
 */
export function showPopMenu(anchorEl, items, align = "left", follow = false) {
    const toggled = current?.classList.contains("open") === true && currentAnchor === anchorEl; // 공용 처리로 이미 닫힌(open 이 빠진) 메뉴는 다시 연다
    closePopMenu();
    if (toggled)
        return;
    if (items.length === 0)
        return;
    const menu = document.createElement("ul");
    menu.className = "treeMenu open active";
    setTimeout(() => menu.classList.remove("active"));
    items.forEach(m => {
        const li = document.createElement("li");
        if (m.divider) {
            li.className = "divider";
        }
        else {
            if (m.danger)
                li.classList.add("danger");
            if (m.muted)
                li.classList.add("muted");
            if (m.icon) {
                li.innerHTML = `<i class="${m.icon}"></i>`;
                if (m.iconColor && /^\w+$/.test(m.iconColor))
                    li.querySelector("i").style.color = `var(--${m.iconColor})`;
            }
            // 문구는 글자로만 넣는다 (사용자 이름 같은 값이 들어와도 HTML 로 해석되지 않게)
            (m.cols ?? [m.text || ""]).forEach(text => { const span = document.createElement("span"); span.textContent = text; if (m.cols)
                span.dataset.col = ""; li.appendChild(span); });
            li.addEventListener("click", e => {
                e.stopPropagation();
                closePopMenu();
                m.action?.();
            });
        }
        menu.appendChild(li);
    });
    // 기준 요소가 스크롤 영역 안에 있으면 메뉴도 그 영역 안(position:absolute)에 넣어 기준 요소 자리에 붙은 채 함께 스크롤되게 하고,
    // 없으면 body 에 붙여 문서 스크롤 값을 더해 배치한다 (스크롤 영역 밖(body)에 두면 영역이 스크롤될 때 메뉴만 제자리에 남는다)
    const host = follow ? scrollHostOf(anchorEl) : null;
    if (host && getComputedStyle(host).position === "static")
        host.style.position = "relative";
    // 스크롤 영역의 직계 자식에 프레임 폭(최소·최대 폭, 가운데 정렬)을 주는 페이지 레이아웃 규칙이 메뉴에도 걸리지 않도록 크기를 직접 지정한다
    if (host)
        Object.assign(menu.style, { width: "max-content", minWidth: "10.5em", maxWidth: "none", margin: "0", flex: "none" });
    (host ?? document.body).appendChild(menu);
    // 기준 요소가 모바일 사이드 패널처럼 메뉴보다 높은 층(z-index)에 있으면 메뉴가 그 뒤에 가려지므로, 기준 요소의 조상 중 가장 높은 층보다 한 단계 위로 올린다
    for (let el = anchorEl; el; el = el.parentElement) {
        const z = Number(getComputedStyle(el).zIndex);
        if (Number.isFinite(z) && z >= Number(getComputedStyle(menu).zIndex))
            menu.style.zIndex = String(z + 1);
    }
    current = menu;
    currentAnchor = anchorEl;
    // 열(cols)이 있는 항목은 같은 순서의 열을 가장 넓은 값에 맞춰 줄 사이에 정렬한다
    const rows = [...menu.querySelectorAll("li")].map(li => [...li.querySelectorAll("span[data-col]")]);
    for (let c = 0; c < Math.max(0, ...rows.map(r => r.length)); c++) {
        const width = Math.max(...rows.map(r => r[c]?.offsetWidth ?? 0));
        rows.forEach(r => { if (r[c])
            r[c].style.minWidth = `${width}px`; });
    }
    const rect = anchorEl.getBoundingClientRect();
    const origin = host ? host.getBoundingClientRect() : { top: -window.scrollY, left: -window.scrollX };
    const [offsetTop, offsetLeft] = host ? [host.scrollTop - host.clientTop, host.scrollLeft - host.clientLeft] : [0, 0];
    const place = (clientTop, clientLeft) => {
        menu.style.top = `${clientTop - origin.top + offsetTop}px`;
        menu.style.left = `${clientLeft - origin.left + offsetLeft}px`;
    };
    place(rect.bottom + 4, rect.left); // 기본은 버튼 좌측 아래 정렬
    // 오른쪽 기준: 메뉴 우측 끝을 기준 요소 우측 끝에 맞춘다 (왼쪽 화면 밖으로 나가면 화면 왼쪽 끝으로 보정)
    if (align === "right")
        place(rect.bottom + 4, Math.max(0, rect.right - menu.getBoundingClientRect().width));
    // 화면 밖 보정: 우측을 벗어나면 버튼 우측 끝에 맞춰 좌측으로 펼치고, 하단을 벗어나면 버튼 위로 띄운다
    requestAnimationFrame(() => {
        const menuRect = menu.getBoundingClientRect();
        if (align === "left" && menuRect.right > window.innerWidth)
            place(rect.bottom + 4, rect.right - menuRect.width);
        if (menuRect.bottom > window.innerHeight)
            place(rect.top - menuRect.height - 4, parseFloat(menu.style.left) + origin.left - offsetLeft);
    });
}
//# sourceMappingURL=PopMenu.js.map