# BitPlus.Web.Site wwwroot/ts/modals (공용 모달)

`IModal`(`loadAsync` → `render`) 을 구현하며 `common.modal.append(name, args)` 로 엽니다. 닫힐 때 `closer(값)` 의 값을 그대로 반환합니다.

| 파일 | 호출 | 인자 | 반환 |
| :--- | :--- | :--- | :--- |
| `messageBox.ts` | `common.alert/info/confirm` | title, content, messageType | 확인 true / 취소 false |
| `langInput.ts` | `common.langInput(values, title)` 또는 `[data-lang]` 옆 버튼 | values({문화권: 값}), title(`@키` 는 다국어 조회, 기본 `@LangInput`) | {문화권: 값} / 취소 null. 문화권·문구는 `api/system/langinput` 한 번으로 조회 |
| `groupSelect.ts` | `common.modal.append('groupSelect', args)` | max, includeHidden, selectedKeys, disableKeys, disableChildren(하위까지 선택 불가, 순환 방지용), title | {그룹키: 표시명} / 취소 null |
| `checkConfirm.ts` | `common.checkConfirm(content, items, checked, title)` | content, items({키: 문구}), checked, title — 문구는 `@키` 로 다국어 | 체크된 옵션 키 배열 / 취소 null. 삭제 등 추가 확인용(일반 메시지 창과 별개) |
| `pagedSelect.ts` | (베이스, 직접 열지 않음) | 검색 + 페이징 선택 모달 공통 — 하위 클래스는 `listUrl`/`titleKey`/`icon`/`rootClass` 만 지정 | — |
| `userSelect.ts` | `common.modal.append('userSelect', args)` | max, disableKeys, selected({키: 표시명}), pageSize, title | {사용자키: 표시명} / 취소 null. 목록에 표시명 + 사용자 ID, `api/membership/user/list` |
| `roleSelect.ts` | `common.modal.append('roleSelect', args)` | max, disableKeys, selected({키: 표시명}), pageSize, title | {역할키: 표시명} / 취소 null. 검색 + `makePager` 페이징, 페이지를 넘겨도 선택 유지 |
| `menuEdit.ts` | `common.modal.append('menuEdit', args)` | editKey(수정할 메뉴 키, 없으면 신규), parentKey(신규 상위 메뉴), values(신규 초기값: key/dpNames/type/icon/extend/action/enabler/accesses[{type,key,name}]) | 저장 `{ key }` / 삭제 `{ key, deleted: true }` / 취소 null. 탭(기본 정보·접근 권한), 저장·삭제 API(`api/sitemap/menu`)까지 모달이 처리. 시스템 메뉴는 형식·경로·사용·접근 권한 잠금·삭제 불가 |
| `periodEdit.ts` | `common.modal.append('periodEdit', args)` | start/end(ISO UTC, 없으면 제한 없음), title/icon/startLabel/endLabel/info(문구 교체, 생략 시 발행 기간 문구, info "" 이면 하단 안내 숨김) | `{ start, end }`(ISO UTC 또는 null) / 취소 null. 로컬 시간대로 입력, 비우면 그쪽 제한 없음, 종료가 시작보다 빠르면 거부 |
| `boardDelete.ts` | `common.modal.append('boardDelete', { ruleNo })` | ruleNo | 삭제 요청 true / 취소 null. 딸린 데이터 수(`api/content/settings/delete`)와 글 처리(모두 삭제 / 같은 형식 게시판으로 옮긴 뒤 삭제) 선택, 영구 삭제 확인 체크 후 `DELETE api/content/settings`. `checkConfirm` 스타일 사용 |
| `sideEdit.ts` | `common.modal.append('sideEdit', args)` | hidden, position(Left/Right), menuKey(빈 값 = 자동), menus(`[{key,text,depth}]` 사용자 메뉴 트리) | `{ hidden, position, menuKey }` / 취소 null. 게시판 설정의 사이드 구성과 같은 항목(사이드 감춤·위치·연결 메뉴), 저장은 부르는 쪽 |
| `targetEdit.ts` | `common.modal.append('targetEdit', args)` | title, info, items(`[{type,key,name}]`) | 대상 목록 `[{type,key,name}]`(비어 있을 수 있음) / 취소 null. 역할/그룹/하위 포함 그룹/사용자 추가·삭제(읽기 권한 등) |
