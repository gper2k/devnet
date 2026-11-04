# DevNet CMS

English | [한국어](README.ko.md)

A website CMS built on ASP.NET Core.

It started as a personal base development pack containing the features a website usually needs. While building it, adding a few more features made it usable as a CMS, so it is packaged and distributed in CMS form.

Information and user communication are supported through [DevNet.biz](https://DevNet.biz).

## Features

- **Setup wizard** (`/Setup`): DB, SMTP and Redis connection tests, database creation and migrations, default data (roles, admin menus, default boards, default pages)
- **Members**: sign-in, sign-up, ID/password recovery, email verification, profile, notifications, messages, my posts, role-based authorization policies
- **External sign-in**: Google, Microsoft, Kakao, Naver, standard OpenID Connect
- **Boards**: default, gallery, forum, manual, FAQ, survey and blog types, per-board file rules, chunked upload for large attachments (up to 2 GB)
- **Pages and home**: view-only pages written by admins (with translations), widget-based home and per-menu portals
- **Unified search**: boards, posts, pages
- **Admin console** (`/Manager`): members, groups, roles, terms, mail templates, boards, reports, pages, menus, popups, background services, system settings, logs
- **AI integration**: OpenAI-compatible, Claude and Gemini connections (used by the content summary service, among others)
- **Localization**: UI text resources in English (default) and Korean, plus localized text stored in the DB
- **REST API**: `/scalar` docs in the development environment, OpenAPI spec at `/openapi/v1.json`

## Tech stack

- .NET 10 (`net10.0`), ASP.NET Core MVC + REST API, Razor views
- Entity Framework Core 10, ASP.NET Core Identity
- Databases: SQL Server (built in), MariaDB 10.6+ (`BitPlus.Data.MariaDb`, `Microting.EntityFrameworkCore.MySql`)
- Optional: Redis (`StackExchange.Redis`)
- Front end: TypeScript (compiled with `npx tsc` during build), SCSS

## Repository layout

| Path | Contents |
| :--- | :--- |
| `CMS.slnx` | Solution |
| `BitPlus.Web.Site/` | The website project that runs ([README](BitPlus.Web.Site/README.md), Korean) |
| `BitPlus.Data.MariaDb/` | MariaDB database provider ([README](BitPlus.Data.MariaDb/README.md), Korean) |
| `libs/` | Shared libraries `BitPlus.dll`, `BitPlus.Web.dll` (referenced as built DLLs via `BitPlus.Libs.props`) |
| `Directory.Build.props` | Common properties, versioning (MinVer, git tags prefixed with `v`) |
| `LICENSE.txt` | MIT license |

## Running and installing

Requirements: .NET 10 SDK, Node.js (`npx` compiles TypeScript), SQL Server or MariaDB.

```bash
dotnet run --project BitPlus.Web.Site
```

- The default address is `http://localhost:5500` (the `https` profile uses `https://localhost:5501`).
- Until installed, every request goes to the setup wizard (`/Setup`). The wizard creates the database, writes `appsettings.Setup.json` and restarts the site.
- A `setup.json` in the site folder prefills the wizard. `appsettings.Setup.json` and `setup.json` contain connection details, so do not commit them. Both are excluded from publish output.
- An installed site applies pending migrations on startup.
- For reverse proxy settings, folder layout and how to extend the site, see [BitPlus.Web.Site/README.md](BitPlus.Web.Site/README.md).

## License

- This project is licensed under the [MIT License](LICENSE.txt).
- Copyright notices and full license texts for the NuGet packages and `wwwroot/lib` libraries used are in [BitPlus.Web.Site/THIRD-PARTY-NOTICES.txt](BitPlus.Web.Site/THIRD-PARTY-NOTICES.txt).

## Contact and support

Documentation, questions and feedback: [https://DevNet.biz](https://DevNet.biz)
