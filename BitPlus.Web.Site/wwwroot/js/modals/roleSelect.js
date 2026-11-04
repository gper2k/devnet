import PagedSelect from "./pagedSelect.js";
/**
 * 검색 + 페이징 목록에서 체크상자로 역할을 선택하는 공용 역할 선택 모달 (인자/반환은 PagedSelect 참고).
 * common.modal.append("roleSelect", args) 로 열며, 확인 시 { 역할키: 표시명 } 객체, 취소 시 null 로 닫힌다.
 */
export default class RoleSelect extends PagedSelect {
    listUrl = "/api/membership/role/list";
    titleKey = "RoleSelect";
    icon = "fiPeopleCheck";
    rootClass = "roleSelect";
}
//# sourceMappingURL=roleSelect.js.map