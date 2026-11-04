# Models/Manage

관리 API(`Controllers/API/ManageController*`, 토큰으로 사이트를 관리하는 AI 에이전트·외부 자동화용)의 요청 본문(JSON) 모델.

| 파일 | 용도 |
| :--- | :--- |
| `ManagePostSaveRequest.cs` | 글 등록·수정 (`Culture` = 다국어 게시판 새 글의 원문 언어) |
| `ManageMenuSaveRequest.cs` | 메뉴 저장 |
| `ManageConfigSaveRequest.cs` | 설정 저장 |
| `ManageUserRoleRequest.cs` | 회원 역할 변경 |
| `ManageUserStatusRequest.cs` | 회원 상태 변경 |
