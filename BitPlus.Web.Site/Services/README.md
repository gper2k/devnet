# BitPlus.Web.Site/Services

사이트 기본 서비스 폴더입니다. 여기 있는 `IService` 구현(보통 `BitPlus.Web.Services.ServiceBase` 상속)은 시작할 때 자동으로 등록되어 관리자 "서비스" 화면(/Manager/Service)에 나오고, 관리자가 켜야 동작합니다. 틀은 `BitPlus.Web/Services/README.md` 를 봅니다.

## 파일

| 파일 | 설명 |
|---|---|
| `ErrorAlertService.cs` | 오류 알림: `system.error` 때 최종 관리자(직접 역할 + 소속 그룹 역할) 메일로 심각도·시각·요청·사용자·오류 형식·내용·상세(스택) 정리. 같은 오류(형식 + 경로)는 설정 `interval`(분, 비우면 10, 0 이면 매번) 안에 한 번만, 건너뛴 횟수는 다음 메일에. 메일 문구는 사이트 기본 언어. 설정 양식 `Views/Manager/Service/Forms/ErrorAlertService.cshtml`. |
| `SystemManagers.cs` | 최종 관리자 역할을 직접 가졌거나 그 역할의 그룹에 속한 활성 사용자 질의(`Of`). |
| `ContentSummaryService.cs` | 컨텐츠 AI(검사 → 요약): 글 등록·수정(`content.created`/`content.updated`, 웹 화면·관리 API 모두 `ContentManager.SavePostAsync` 에서 발행), 번역 저장(`content.translated`, 값 culture), 관리자 요약 갱신 요청(`content.summary`)을 한 큐에서 차례로 처리(AI 서버가 하나여도 호출이 겹치지 않게). 글: 1) 검사(`review` 켬, 요약 갱신 요청 제외) — 보기 전용 페이지를 뺀 모든 글을 판정(JSON flag·reason), 걸리면 사유를 글 속성 `ReviewReason` 에 남기고 감춤(`notifyOnly` 면 알리기만), 최종 관리자·글쓴이에게 알리고 멈춤. 2) 요약 — 일반·포럼 게시판 글 본문(HTML 제거·공백 정리, 최대 2만 자)이 `plainLength`(비우면 200, 0 이면 항상 AI) 이하이면 그대로, 넘으면 AI 요약(글과 같은 언어)을 글 속성 `Summary` 에. 번역 저장: 일반·포럼 게시판이면 그 번역본을 같은 규칙으로 요약해 `ContentTranslations.Summary` 에(번역 검사는 하지 않음 — 번역은 원문을 고칠 수 있는 사람만 저장). 번역 자체는 하지 않음(수동). 설정 `aiConnection`(요약), `reviewAiConnection`(비우면 요약과 같은 접속), `prompt`, `plainLength`, `review`, `notifyOnly`, `criteria`. 설정 양식 `Views/Manager/Service/Forms/ContentSummaryService.cshtml`. |

## 새 서비스 추가

1. 이 폴더에 `ServiceBase` 상속 클래스 — `Title`·`Description`(리소스), `Events`(구독 이벤트), `HandleAsync`(작업 하나).
2. 설정이 필요하면 `Views/Manager/Service/Forms/{클래스 이름}.cshtml` 양식 조각(입력 `name` = 설정 값 키).
3. 명칭·설명 리소스 키를 `Properties/Resources*.resx` 에 추가.
