# BitPlus.Web.Site/OAuth (외부 로그인)

관리자 "계정 관리 > OAuth 구성" 에 등록한 서비스(Google, Microsoft, Kakao, Naver, 일반 OIDC)로 로그인·가입·계정 연결을 처리합니다. 추가 패키지 없이 ASP.NET Core 기본 OAuth 처리기(`OAuthHandler<OAuthOptions>`)만 씁니다.

| 파일 | 역할 |
| :--- | :--- |
| `OAuthKind.cs` | 서비스 종류 |
| `OAuthPreset.cs` | 종류별 고정 값: 인증·토큰·사용자 정보 주소, 범위, 사용자 정보에서 읽을 경로(id·메일·검증 여부·이름), PKCE |
| `OAuthProvider.cs` | 서비스 하나 (설정 `OAuthProviders` 목록, `ListOf`/`EnabledOf`/`SchemeOf`). Secret 은 `ProtectPurpose` 용도 키로 암호화. 스킴 이름 `oauth.{Id}.{UDate}`, 콜백 `/signin-oauth/{Id}` |
| `OAuthSchemeProvider.cs` | 스킴 공급자 — 설정의 사용 중 서비스를 스킴으로 더함. 요청 처리 스킴은 콜백 주소 요청일 때만 더함 |
| `OAuthOptionsSetup.cs` | 스킴 이름으로 서비스를 찾아 처리기 옵션(Client ID/Secret, 주소, 범위, PKCE, 보호기, 이벤트)을 채움 |
| `OAuthLoginEvents.cs` | 사용자 정보 조회 → 클레임, 확인 끝나면 `ExternalIdentity` 보관 후 `/Account/External`, 실패·취소는 `/Account/SignIn?external=failed` |
| `ExternalIdentity.cs` | 외부 사용자 정보 — 캐시(10분) + HttpOnly 쿠키(`BitPlus.External`)로 가입·연결 화면까지 전달 |
| `MasterKeyProtector.cs` | state 보호기 — 마스터 키 파생 키(AES-GCM). Data Protection 키는 서버마다 달라 쓰지 않음 |
| `OAuthLoginExtend.cs` | `AddOAuthLogin()` — 설치 후 `Program.cs` 에서 등록 |

## 흐름
1. 로그인 화면 버튼(`Views/Account/_ExternalLogins`) → `GET /Account/ExternalLogin?provider=` → 서비스 인증 화면 (Challenge).
2. 콜백 `/signin-oauth/{Id}` 를 `UseAuthentication` 이 처리 → `OAuthLoginEvents` 가 쿠키 로그인 없이 `ExternalIdentity` 보관 → `/Account/External`.
3. `/Account/External` = 회원가입 뷰의 외부 로그인 모드. 화면이 `POST api/account/external` 호출:
   - 로그인 중 → 지금 계정에 연결 (`UserLogins`: LoginProvider = 서비스 Id, ProviderKey = 서비스 안 사용자 id).
   - 연결된 계정 있음 → 일반 로그인과 같은 처리(비활성·잠금·메일 검증).
   - 없음 → 비밀번호 없는 가입 양식 → `POST api/account/external/join` (서비스가 검증한 메일을 그대로 쓰면 메일 인증 완료).
4. 같은 메일의 기존 계정에 자동 연결하지 않음 — 기존 회원은 로그인 후 내 정보(`/user`)에서 연결.

## 저장 즉시 반영
- 서비스 목록은 설정 캐시에서 바로 읽고, 저장하면 `UDate` 가 바뀌어 스킴 이름(= 옵션 캐시 키)이 바뀜 → 재시작·서버 간 동기화 불필요.
- 기본 인증 스킴은 없음 — 로그인 사용자는 `UseAccount` 가 자체 토큰으로 넣음.

## 서비스 추가
`OAuthKind` 값 → `OAuthPreset.Of` 에 주소·경로 → 관리 화면 종류 선택(`Views/Manager/Account/OAuth.cshtml`). Apple 은 Client Secret 을 개인 키(p8)로 서명한 JWT 로 만들어야 해서 아직 없음.
