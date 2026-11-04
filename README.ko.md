# DevNet CMS

[English](README.md) | 한국어

ASP.NET Core 기반 웹사이트 CMS 입니다.

처음엔 사이트 개발에 필요한 기능을 모두 담은 개인용 사이트 기본 개발팩으로 만들기 시작했습니다. 작성하는 중에 기능을 조금 더하면 CMS 로 쓸 수 있겠다고 판단해 CMS 형태로 정리해 배포합니다.

필요한 정보와 사용자 소통은 [DevNet.biz](https://DevNet.biz) 에서 지원합니다.

## 주요 기능

- **설치 마법사** (`/Setup`): DB·SMTP·Redis 연결 테스트, DB 생성과 마이그레이션, 기본 데이터(역할·관리자 메뉴·기본 게시판·기본 페이지) 등록
- **회원**: 로그인·회원 가입·아이디/비밀번호 찾기·메일 인증, 내 정보·알림·쪽지·내 글, 역할 기반 권한 정책
- **외부 로그인**: Google, Microsoft, Kakao, Naver, 표준 OpenID Connect
- **게시판**: 일반·갤러리·포럼·메뉴얼·FAQ·설문·블로그 형식, 게시판별 파일 규칙, 큰 첨부 파일 분할 업로드(최대 2GB)
- **페이지·홈**: 관리자가 작성하는 보기 전용 페이지(번역 지원), 위젯 배치 홈·메뉴별 포탈
- **통합 검색**: 게시판·글·페이지
- **관리자 콘솔** (`/Manager`): 회원·그룹·역할, 약관·메일 템플릿, 게시판·신고·페이지, 메뉴·팝업, 백그라운드 서비스, 시스템 설정·로그
- **AI 연동**: OpenAI 호환·Claude·Gemini 접속 (컨텐츠 요약 서비스 등)
- **다국어**: 화면 문구 영어 기본·한국어 리소스, DB 다국어 문장
- **REST API**: 개발 환경에서 `/scalar` 문서, OpenAPI 규격 `/openapi/v1.json`

## 기술 스택

- .NET 10 (`net10.0`), ASP.NET Core MVC + REST API, Razor 뷰
- Entity Framework Core 10, ASP.NET Core Identity
- DB: SQL Server(기본 포함), MariaDB 10.6 이상(`BitPlus.Data.MariaDb`, `Microting.EntityFrameworkCore.MySql`)
- 선택: Redis (`StackExchange.Redis`)
- 프런트엔드: TypeScript(빌드 때 `npx tsc` 로 컴파일), SCSS

## 저장소 구성

| 경로 | 내용 |
| :--- | :--- |
| `CMS.slnx` | 솔루션 |
| `BitPlus.Web.Site/` | 실행되는 웹사이트 프로젝트 ([README](BitPlus.Web.Site/README.md)) |
| `BitPlus.Data.MariaDb/` | MariaDB DB 프로바이더 ([README](BitPlus.Data.MariaDb/README.md)) |
| `libs/` | 공용 라이브러리 `BitPlus.dll`, `BitPlus.Web.dll` (빌드된 dll 로 참조, `BitPlus.Libs.props`) |
| `Directory.Build.props` | 공통 속성, 버전(MinVer, `v` 접두 git 태그) |
| `LICENSE.txt` | MIT 라이선스 |

## 실행과 설치

필요한 것: .NET 10 SDK, Node.js(`npx` 로 TypeScript 컴파일), SQL Server 또는 MariaDB.

```bash
dotnet run --project BitPlus.Web.Site
```

- 기본 실행 주소는 `http://localhost:5500` 입니다(`https` 프로필은 `https://localhost:5501`).
- 설치 전에는 모든 요청이 설치 마법사(`/Setup`)로 이동합니다. 마법사가 DB 를 만들고 `appsettings.Setup.json` 을 쓴 뒤 사이트를 다시 시작합니다.
- 사이트 폴더의 `setup.json` 은 마법사 입력값을 미리 채웁니다. `appsettings.Setup.json` 과 `setup.json` 에는 접속 정보가 들어가므로 저장소에 올리지 마세요. 두 파일은 게시(Publish) 결과에서도 빠집니다.
- 설치된 사이트는 시작할 때 적용하지 않은 마이그레이션을 적용합니다.
- 리버스 프록시 설정, 폴더 구성, 확장 방법은 [BitPlus.Web.Site/README.md](BitPlus.Web.Site/README.md) 를 참고하세요.

## 라이선스

- 이 프로젝트는 [MIT 라이선스](LICENSE.txt) 입니다.
- 사용한 NuGet 패키지와 `wwwroot/lib` 라이브러리의 저작권 고지와 라이선스 전문은 [BitPlus.Web.Site/THIRD-PARTY-NOTICES.txt](BitPlus.Web.Site/THIRD-PARTY-NOTICES.txt) 에 있습니다.

## 문의와 지원

문서, 질문, 의견은 [https://DevNet.biz](https://DevNet.biz) 에서 받습니다.
