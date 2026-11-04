# BitPlus.Web.Site Views/View (컨텐츠 보기)

`ViewController`(`/view/{contentNo}`)의 뷰. **보기 전용(`OnlyView`) 컨텐츠 규칙**(사이트에 하나만 허용, 설치 시 "PageView")에 등록된 글 하나를 바로 출력해, 컨텐츠 등록만으로 사이트 페이지를 늘려 가는 기능입니다. (형식별 파샬뷰로 목록/상세를 그리는 `/content/{ruleNo}/{view?}` 는 `Views/Content` 참고)

| 파일 | 설명 |
| :--- | :--- |
| `Index.cshtml` | 제목·등록일과 본문. 본문(HTML)은 관리자가 관리하는 컨텐츠라 정제·샌드박스 없이 `Html.Raw` 로 그대로 출력. 관리자에게는 하단 중앙에 편집 버튼. 모델 `ContentViewModel`. |
| `Edit.cshtml` | 작성/수정 화면(`/view/edit` 신규, `/view/edit/{글 번호}` 그 글 수정). 제목·SunEditor 본문(이미지 소유자 `content.view`/글 식별키, 신규는 임시 키)·공개 여부, 제목 옆 ⋮ 메뉴로 게시 기간(`periodEdit`)·읽기 권한(`targetEdit`, `ContentReadRule`) 설정, 저장은 `api/content/pageview`. 관리자 전용. 모델 `ContentEditModel`. |

## 동작

1. `OnlyView` 이고 활성인 컨텐츠 규칙을 찾음 (없으면 404).
2. 그 규칙(`ContentRuleId`)의 글 중 `ContentNo == contentNo` 를 찾음.
3. 공개 조건: `Visible` 이고 `Published`, 게시 기간(`Start`~`End`, 비면 제한 없음) 안. (관리자 정책을 만족하는 사용자는 게시 기간 제한이 없음) 조건에 안 맞거나 글이 없으면 404. (승인 같은 규칙 옵션은 속성으로 관리하기로 해 추후 속성 기반으로 추가)
3-1. 글에 읽기 권한(`ContentReadRule`)이 있으면 그 대상(사용자·그룹·역할)에 속한 로그인 사용자와 관리자만 보고, 아니면 403. 비어 있으면 모두 읽기 가능.
4. 본문(`ContentBody`)을 정제·샌드박스 없이 그대로 출력 (컨텐츠는 관리자가 관리하므로 스크립트·임베드 포함 가능. 일반 사용자가 쓰는 글에는 이 페이지를 쓰지 말 것).

아직 없는 것: 규칙 단위 읽기 권한(`AccessRule`) 검사, 조회수 증가·조회 기록, 댓글/좋아요, 첨부 파일. 컨텐츠 본문 기능과 함께 추가합니다.
