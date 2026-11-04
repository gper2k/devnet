# BitPlus.Web.Site.ValidConfigs 폴더 개요

## 1. 폴더 역할 및 책임
`BitPlus.Web.Site.ValidConfigs`는 API 요청 페이로드의 유효성을 런타임에 동적으로 검증하기 위한 **JSON 검증 규칙 스키마 파일들**을 보관합니다.
`[Valid("파일명.json")]` 어트리뷰트와 결합되어, 클라이언트의 입력값이 비즈니스 로직에 전달되기 전에 필수값 여부, 문자열 길이, 형식(이메일, 정규식), 다국어 변환 등을 엄격하게 심사합니다.

---

## 2. 파일 목록 및 구현 상세

| 파일명 | 대상 DTO / 액션 | 포함될 검증 규칙 |
| :--- | :--- | :--- |
| `UserSave.json` | 사용자 생성/수정 (`POST/PUT api/membership/user`) | • `userName`: 필수, 최대 32자 (최소 길이는 `AccountRule.UidMinSize` — 화면은 입력항 `data-min`, 서버는 `CreateUser`에서 검증)<br>• `email`: 필수, 이메일 형식, 최대 256자<br>• `phoneNumber`: 선택, 숫자/`+`/`-`/괄호/공백, 최대 32자 |
| `GroupSave.json` | 그룹 생성/수정 (`POST/PUT api/membership/group`) | • `name`: 필수, 최소 2자 이상, 최대 64자<br>• `grade`: 선택, 최대 4자<br>• `dpName`: 필수, 다국어 컨버터 적용 |
| `LoginRequest.json` | 로그인 요청 (`POST api/auth/login`) | • `userId`: 필수 입력<br>• `password`: 필수 입력, 최소 6자 이상 |
| `BoardSave.json` | 게시판 마스터 생성/수정 | • `boardKey`: 필수, 영문/숫자 정규식, 최소 2자<br>• `name`: 필수, 최소 2자<br>• `maxAttachmentCount`: 0~10 범위 숫자 |
| `PostSave.json` | 게시글 작성/수정 (`POST/PUT api/board/{key}/posts`) | • `title`: 필수, 최소 2자, 최대 200자<br>• `content`: 필수 본문 입력<br>• `contentType`: "Markdown" 또는 "Html" 허용 |
| `CommentSave.json` | 댓글 작성 (`POST api/board/{key}/posts/{id}/comments`) | • `content`: 필수, 최소 1자, 최대 1,000자 |

---

## 3. 스키마 포맷 예시 (PostSave.json)
```json
[
  { "name": "title", "displayName": "@Title", "filters": { "required": [], "minLength": [2], "maxLength": [200] } },
  { "name": "content", "displayName": "@Content", "filters": { "required": [] } },
  { "name": "contentType", "displayName": "@ContentType", "filters": { "required": [] } }
]
```

---

## 4. 코딩 및 설정 규칙
- JSON 파일 속성은 소문자 카멜 케이스(`camelCase`) 준수.
- `displayName`에 `@` 접두사를 붙여 `Resources` 다국어 키와 자동 연동.
