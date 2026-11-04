# BitPlus.Web.Site/Ai (AI 접속·프로바이더)

관리자 "외부 서비스 > AI API" 에 등록한 접속 정보로 AI 를 호출하는 클래스. 기능(예: 게시물 요약)은 `AiClient` 만 쓰면 되고, 형식별 차이는 프로바이더가 맡습니다.

| 파일 | 역할 |
| :--- | :--- |
| `AiConnection.cs` | 접속 정보 하나 (설정 `AiConnections` 목록, `ListOf(config)`). API 키는 `ProtectPurpose` 용도 키로 암호화 저장 |
| `AiFormat.cs` | 요청 형식: `OpenAI`(호환: Ollama·vLLM·LM Studio), `Claude`, `Gemini` |
| `AiMessage.cs` | 형식 공용 대화 메시지 (`User`/`Assistant`) |
| `AiProvider.cs` | 프로바이더 기반 — 공용 `HttpClient`, 전송·오류 처리(`SendAsync` → `Result<string>`), 형식별 인스턴스 `Of(format)` |
| `OpenAiProvider.cs` | `POST {주소}/chat/completions`, `Authorization: Bearer`. `top_k` 는 값이 있을 때만, 추론 강도를 쓰면 최대 토큰을 `max_completion_tokens` 로 |
| `ClaudeProvider.cs` | `POST {주소}/messages`, `x-api-key`, `anthropic-version: 2026-10-21`. 이 버전은 온도·Top P·Top K 를 받지 않아 보내지 않음. 추론 강도 = `output_config.effort`, 최대 토큰 필수(비우면 16000). 거절(`refusal`)은 실패 |
| `GeminiProvider.cs` | `POST {주소}/models/{모델}:generateContent`, `x-goog-api-key`. `systemInstruction`, `generationConfig`(추론 강도 = `thinkingConfig.thinkingLevel`), 생각(thought) 부분 제외 |
| `AiClient.cs` | 접속 하나로 만든 커넥션 — `Create(config, id?)`(비우면 기본 접속, 없으면 사용 중인 첫 접속), `AskAsync(system, prompt)`, `CompleteAsync(system, messages)` |

## 사용
```csharp
var ai = AiClient.Create(this.Config!);                     // 기본 접속 (없으면 null)
var result = await ai!.AskAsync("너는 게시물 요약기다. 한국어 3줄로 요약한다.", 본문);
if (result.Success) 요약 = result.Data;                      // 실패 시 result.Message
```
- 시스템(초기화) 프롬프트 = 역할·규칙·출력 형식, 명령(사용자 메시지) = 처리할 내용. 이전 대화를 이어 갈 때는 `CompleteAsync` 에 `AiMessage` 목록을 넘깁니다.
- 형식 추가: `AiFormat` 에 값 → `AiProvider` 상속 클래스(`BuildRequest`/`ReadText`) → `AiProvider` 정적 생성자의 형식별 목록에 등록 → 화면 형식 선택(`Views/Manager/External/Ai.cshtml`).
