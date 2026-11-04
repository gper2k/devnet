# Views/Setup

설치 마법사 화면 (`SetupController` → `/Setup`). 설치 전에는 DB 가 없으므로 공용 레이아웃(_Layout, DB 설정을 읽음)을 쓰지 않는 단독 페이지입니다.

| 파일 | 내용 |
| :--- | :--- |
| `Index.cshtml` | 마법사: 라이선스 동의 → DB(프로바이더 선택·입력 항목·접속 테스트 필수) → SMTP(선택, 테스트 메일) → Redis(선택, PING) → 설치 준비(요약) → 설치(진행바·단계 상태, 실패 시 재시도/설정으로 돌아가기, 완료 후 재시작 대기). 입력값은 단계 API 마다 전부 보냄. `setup.json` 이 있으면 입력을 미리 채움. |
| (스타일) | 전용 스타일 `wwwroot/css/Setup.scss` → `setup.css`/`setup.min.css` (compilerconfig). 왼쪽 단계 사이드바 + 오른쪽 입력 패널 카드, 좁은 화면은 위아래 배치. 테스트 결과·설치 단계 상태는 `data-state`(ok/fail/run). |

## setup.json (사이트 루트, 선택)
```json
{
  "database": { "provider": "SqlServer", "server": "", "port": "1433", "database": "", "user": "", "password": "", "trustCertificate": true, "prefix": "APP" },
  "smtp": { "host": "", "port": 587, "ssl": true, "user": "", "password": "", "from": "", "fromName": "", "testTo": "" },
  "redis": ""
}
```
비밀번호가 들어가므로 운영 배포에는 두지 않습니다(개발 중 재설치 편의용).
