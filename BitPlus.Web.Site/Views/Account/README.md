# Views/Account

계정 인증 화면 (레이아웃: `Shared/_AccountLayout.cshtml`).

- `SignIn.cshtml` — 로그인 (사용자 ID 또는 이메일, 비밀번호, AJAX 비동기 인증 처리 `POST /api/account/signin`).
- `SignUp.cshtml` — 회원가입 (사용자 ID, 이메일, 비밀번호, 서비스 이용약관 및 개인정보 처리방침 동의 문구, `POST /api/account/join`, 약관 조회 `GET /api/account/terms`).
  외부 로그인 모드(`/Account/External`): 비밀번호 칸 없이 `POST /api/account/external`(로그인·연결) → 계정이 없으면 `POST /api/account/external/join`.
- `_ExternalLogins.cshtml` — 로그인 화면의 외부 로그인 버튼 (관리 화면 "OAuth 구성" 의 사용 중 서비스, `/Account/ExternalLogin`).
- `FindAccount.cshtml` — ID/PW 찾기 (아이디 찾기 / 비밀번호 재설정 탭 전환, `POST /api/account/find/id`, `POST /api/account/find/password`).
- `ResetPassword.cshtml` — 비밀번호 재설정 (메일 토큰 검증 및 새 비밀번호 입력 폼, `POST /api/account/password/reset`).
- 모든 문구는 리소스 키(SignIn, SignUp, CreateAccount, FindAccount, FindId, ResetPassword, Password, UidOrEmail, EnterUid, EnterUidOrEmail, EnterPassword, CreatePassword, SignUpAgree, SignInAgree, TermsAndPrivacy, SecurityTerms 등)로 다국어 표시. 동의 문구는 `{0}` 위치에 약관 버튼을 넣는다.
- `VerifyMail.cshtml` — 메일 인증 안내(가린 메일 주소, 발송됨/발송 실패/링크 실패 상태) + 인증 상태 확인(`POST /api/account/mail/check`) + 재발송(`POST /api/account/mail/resend`).
