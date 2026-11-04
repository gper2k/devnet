/**
 * 에디터 공용 규격: 에디터 엔진(SunEditor 등)마다 한 개의 공급자(IEditorProvider)를 두고,
 * 사이트(common)가 에셋 로딩·소유자/이미지 업로드 연동·호출 방식을 공용으로 처리한다.
 * 호출하는 쪽(뷰)은 엔진을 모르고 common.makeEditor(textarea, option) 와 반환값(IEditor)만 쓴다.
 */
export {};
//# sourceMappingURL=IEditorProvider.js.map