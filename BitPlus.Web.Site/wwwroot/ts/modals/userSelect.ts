import PagedSelect from "./pagedSelect.js";

/**
 * 검색 + 페이징 목록에서 체크상자로 사용자를 선택하는 공용 사용자 선택 모달 (인자/반환은 PagedSelect 참고, 예: 담당자 지정).
 * common.modal.append("userSelect", args) 로 열며, 확인 시 { 사용자키: 표시명 } 객체, 취소 시 null 로 닫힌다.
 * 목록에는 표시명과 사용자 ID 가 함께 표시된다.
 */
export default class UserSelect extends PagedSelect {
    protected readonly listUrl = "/api/membership/user/list";
    protected readonly titleKey = "UserSelect";
    protected readonly icon = "fiPerson";
    protected readonly rootClass = "userSelect";
}
