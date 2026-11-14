# AutoElite

[![CI](https://github.com/Obj-Proger/AutoElite/actions/workflows/ci.yml/badge.svg)](https://github.com/Obj-Proger/AutoElite/actions/workflows/ci.yml)

Management system for driving schools: the full student lifecycle from the first enquiry to
graduation, in one self-hosted application.

> **Status: early development.** The domain model, the application layer and the infrastructure are in
> place; the HTTP API is being written; the clients are not written yet. Nothing here is ready for
> production use.

## Planned scope

| Area | Scope |
|---|---|
| Users and branches | Accounts with one or more roles, profiles, deactivation, branches of one school |
| CRM | Leads from the public form, statuses, notes, conversion into a contract |
| Courses, contracts, groups | Course catalog by licence category, contracts, payments due, study groups |
| Theory | Lesson schedule, attendance, learning materials |
| Practice | Vehicles, training grounds, routes, driving slots and bookings, skill grades |
| Testing and exams | Traffic-rules question bank, test templates, timed attempts, theory and practical exams |
| Communication | Personal and group chats (encrypted at rest), in-app notifications, reminders |
| Finance | Payments, refunds, online card payments, multi-currency amounts |
| Staff | Tasks with deadlines and recurrence, document attachments |
| Analytics | Revenue, conversion, contract outcomes, instructor workload; PDF and Excel export |

Roles: administrator, manager, driving instructor, theory instructor, student.

## Architecture

Clean architecture in four layers; dependencies point inwards only.

| Project | Responsibility |
|---|---|
| `AutoElite.Domain` | Entities, value objects, domain events and business rules; no dependencies |
| `AutoElite.Application` | Commands and queries (CQRS) with a small in-house dispatcher and pipeline behaviors |
| `AutoElite.Infrastructure` | Persistence (EF Core, PostgreSQL), identity, background jobs, mail, files, import and reports |
| `AutoElite.API` | Versioned public HTTP API with an OpenAPI document *(in progress)* |

Expected failures are returned as `Result` values with typed errors rather than thrown; validation
errors are reported per field. Each school deploys its own instance (single-tenant).

Planned stack: .NET 10, ASP.NET Core, EF Core with PostgreSQL, Blazor (web), .NET MAUI (mobile).

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and, for the mobile application, the .NET MAUI
workload (see [The mobile application](#the-mobile-application)).

```shell
dotnet build
dotnet test
```

The infrastructure tests need PostgreSQL 18; each test class creates a database of its own and drops it
afterwards. By default they start a throwaway container, which needs a local Docker engine. To use a
PostgreSQL server instead, give the tests the connection string of a user who may create databases:

```shell
# PowerShell
$env:AUTOELITE_TEST_POSTGRES = "Host=...;Port=5432;Username=...;Password=..."
dotnet test
```

The tests of the S3 file storage need an S3-compatible storage; each test class creates a bucket of its own and
removes it afterwards. By default they start SeaweedFS in a throwaway container, which also needs a local Docker
engine; without Docker they are skipped, and CI, which has Docker, runs them on every push. To use a storage already
running instead, such as `weed.exe` of SeaweedFS started locally, give the tests an account that may create buckets:

```shell
# PowerShell
$env:AUTOELITE_TEST_S3 = "ServiceUrl=http://...:8333;AccessKey=...;SecretKey=..."
dotnet test
```

### Running the API

The API needs PostgreSQL and a few settings and secrets of the machine, which stay out of the repository in user
secrets. Generate each key once (in PowerShell: `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`):

```shell
dotnet user-secrets set "ConnectionStrings:AutoElite" "Host=...;Database=autoelite;Username=...;Password=..." --project src/AutoElite.API
dotnet user-secrets set "Jwt:SigningKey" "<a random key>" --project src/AutoElite.API
dotnet user-secrets set "ChatEncryption:CurrentKeyId" "key_1" --project src/AutoElite.API
dotnet user-secrets set "ChatEncryption:Keys:key_1" "<a random key>" --project src/AutoElite.API
dotnet user-secrets set "FileStorage:RootPath" "C:\AutoElite\files" --project src/AutoElite.API
dotnet user-secrets set "Client:BaseUrl" "http://localhost:5173" --project src/AutoElite.API
dotnet user-secrets set "Email:Mode" "PickupDirectory" --project src/AutoElite.API
dotnet user-secrets set "Email:PickupDirectory" "C:\AutoElite\mail" --project src/AutoElite.API
dotnet user-secrets set "Email:FromAddress" "no-reply@autoelite.localhost" --project src/AutoElite.API
dotnet user-secrets set "FirstAdministrator:Email" "admin@autoelite.localhost" --project src/AutoElite.API
dotnet user-secrets set "FirstAdministrator:LastName" "Administrator" --project src/AutoElite.API
dotnet user-secrets set "FirstAdministrator:FirstName" "First" --project src/AutoElite.API
dotnet run --project src/AutoElite.API
```

The API creates the database schema when it starts and invites the first administrator; the invitation is in the mail
folder (see [First start](#first-start)).

With the `PickupDirectory` mode, emails are written as files to that folder instead of being sent; `Client:BaseUrl`
is the address of the web client that the links in emails lead to. In development the API reference (Scalar) opens at
`/scalar`. The OpenAPI document is at `/openapi/v1.json`; `/health/live` and `/health/ready` report
the health of the host and of its database. Errors are [problem details](https://www.rfc-editor.org/rfc/rfc9457):
`detail` in the language of the `Accept-Language` header (English without one), a `code` for clients to choose their
own text by, and the failed fields in `errors`, named as in the JSON of the request.

Behind a reverse proxy, the API takes the client address and scheme from `X-Forwarded-For` and `X-Forwarded-Proto` of
the proxies it trusts: one on the same machine, and those named in `ReverseProxy:KnownProxies` (addresses) or
`ReverseProxy:KnownNetworks` (such as `172.18.0.0/16` of a Docker network). Pages of `Client:BaseUrl` and of the
origins in `Cors:AllowedOrigins` may call the API from the browser. A client may send `RateLimiting:PermitsPerMinute`
requests a minute (600 by default, counted per signed-in user or per address) and
`RateLimiting:SensitivePermitsPerMinute` to signing in, the password reset and the public form (20 by default, per
address); over a limit the API answers 429 with `Retry-After`.

Every response tells browsers not to guess its type, not to show it in a frame and not to send the API's address in
the Referer of a link; outside development it also asks them to reach the API only over HTTPS for a year (HSTS, sent
over HTTPS only), and the answers of `/api/v1/auth`, which carry tokens, are never stored.

The website keeps no refresh token where a script could read it. It signs in with `POST /api/v1/auth/browser/login`,
which answers the access token only, kept in memory, and sets the refresh token as an HttpOnly, Secure,
SameSite=Strict cookie sent only to `/api/v1/auth/browser`; `POST .../refresh` renews the session with it, when the
site starts and when the access token expires, and `POST .../logout` ends it. These endpoints answer only requests with
the header `X-AutoElite-Client: browser`, which no page of another site can send without a CORS preflight the API
refuses. So that the browser sends the cookie, the website and the API must be on the same site, such as
`school.example` and `api.school.example`, and the website at `Client:BaseUrl` or in `Cors:AllowedOrigins`, the only
origins allowed to send credentials.

Administrators watch and retry the background jobs in the Hangfire dashboard at `/jobs`. A browser does not send the
access token when it opens a page, so an administrator first calls `POST /api/v1/jobs-dashboard/session` with it: the
API answers with a cookie that only the dashboard receives and that lasts 30 minutes.

Signed-in users receive updates at once through the SignalR hub at `/hubs/realtime`: `NotificationReceived` for a new
notification and `ChatMessageReceived` for a new message in their chats. The updates carry identifiers only; the client
reads the rest through the API. Browsers send the access token in the `access_token` parameter of the hub's address.

The API tests start the API on a database of their own, on the same PostgreSQL server as the infrastructure tests.

### The client of the API

Every build of the API writes its OpenAPI document to `src/AutoElite.API/obj/openapi.json`. `src/AutoElite.Client`
generates the .NET client of the API from it with [Kiota](https://learn.microsoft.com/openapi/kiota/) (MIT), a local
tool of the repository that the build restores, whenever the document changes; the generated code is in
`src/AutoElite.Client/obj/Kiota` and is not kept in the repository, so the client always matches the API. The
applications of the school register it with `services.AddAutoEliteClient(apiAddress)`: `AuthSessionManager` signs in
and out, and the requests of `AutoEliteApiClient` carry the access token, which is renewed with the refresh token when
it expires or the API rejects it. An application keeps the session in its own `IAuthSessionStore`, such as secure
storage on a device.

### The mobile application

`src/AutoElite.Mobile` is the .NET MAUI application of students, driving instructors and theory instructors, for Android
and Windows. Building it, and so the whole solution, needs the MAUI workload: `dotnet workload install maui`, or the
*.NET Multi-platform App UI development* workload of Visual Studio, which also installs the Android SDK. The application
keeps the session in the secure storage of the device. On the first start it asks for the address of the school's API,
which must use HTTPS; a debug build also takes HTTP and offers the API of the development machine:
`http://localhost:5080` on Windows, `http://10.0.2.2:5080` from the Android emulator. Students and instructors sign in;
the staff of the school work on its website. A student sees their driving and theory lessons on the home page, books a
free practice slot of one of their contracts and cancels a booking, with or without a network, and takes practice tests
and theory exams with the timer and a review of their mistakes, which need a network; rates a completed lesson; and
follows the progress of their training with its payments; pulling the page down sends the waiting actions and reads the
lessons again. A driving instructor sees their practice slots of the next two weeks, cancels a booking before the lesson
and, once it has started, completes it with the hours, the grades of the student's skills and a note, or records that
the student did not come, also without a network. A theory instructor sees their theory lessons and marks the attendance
of a lesson that has started for the whole group at once, in a classroom without a network too. Every user reads their
notifications, whose number on the home page changes at once through the SignalR hub, and their chats, where new
messages appear at once and a message written without a network is sent later. The texts of the application are in
English, with a Russian translation, in `Resources/Strings`; the device's language chooses between them unless the user
chooses one in the settings, where they also change the school and sign out.

The application works without a network too (`src/AutoElite.Client.Offline`). A SQLite database in its private storage
keeps what the user last read, shown as possibly out of date while the API cannot be reached, and the actions the user
takes, such as booking a practice slot, wait in a queue that is sent in order when the device connects to a network or
the application comes back to the foreground. The API decides each action when it receives it: one it refuses, such as
the booking of a slot that another student took meanwhile, stays in the queue for the user to see. What the database
keeps is encrypted with AES-GCM under a key in the secure storage of the device, so a copy of the file alone cannot be
read, and each content is bound to its place. The data on the device belongs to one user: `OfflineDatabase.ClaimAsync`
forgets the data of another user, with its key, when a user signs in, and `ForgetAsync` forgets everything when the user
signs out. Android backups are turned off, and only a debug build may reach the API of the development machine over
HTTP.

### The website

`src/AutoElite.Web` is the website of the school, in Blazor WebAssembly: it runs in the browser, calls the API through
the same generated client as the mobile application and is served as static files. Its design is Noir Modern, like the
application's: `wwwroot/css/tokens.css` copies the tokens of `design/tokens.json` and `wwwroot/fonts` the fonts of
`design/fonts`, and the architecture tests check both copies. Its texts are in `Resources/WebTexts.resx`, in English,
with a Russian translation, and follow the language of the browser. It calls the API of `Api:BaseUrl` in
`wwwroot/appsettings.json`, `http://localhost:5080/` for development, or the site's own address when that is empty. Run
it next to the API with

```bash
dotnet run --project src/AutoElite.Web
```

and open `http://localhost:5173`, the `Client:BaseUrl` of the API, whose pages the API lets call it.

Users of every role sign in on the website (`/sign-in`). The refresh token of its session stays in an HttpOnly cookie of
the API that no script of the page can read, sent only to the endpoints of the browser session with the header of the
website; the access token is kept in the memory of the page and renewed with the cookie (`BrowserSession`), by one tab
of the browser at a time, since the API accepts a refresh token once. The cookie is SameSite=Strict, so in production
the site and the API must be on the same site, such as `school.example` and `api.school.example`, both over HTTPS.

A signed-in user sees the menu of the sections their roles may open (`WebRoles`). Administrators and managers find the
users of the school by name, email address or phone, role and status (`/users`), invite them (`/users/new`), and on the
card of a user correct their name and phone, send the invitation again and deactivate or reactivate them; an
administrator also grants and revokes roles and moves users between branches. A manager works with the students of their
branch, as the API decides, and the errors of the API are shown in the language of the browser. Every page but the front
page and signing in needs signing in, which an architecture test checks; the menu only shows what a role may do, and the
API checks the roles on every request.

The links that the API emails open pages of the website: an invited user sets their first password
(`/accept-invitation`), a user who forgot theirs asks for a link (`/forgot-password`) and sets a new one
(`/reset-password`), and a new email address is confirmed with a button (`/confirm-email-change`), so that a mail
program that opens links to check them does not confirm it. Each page takes the token out of the address as soon as it
has read it, so that it stays neither in the history of the browser nor on the screen.

### First start

When the API starts, it applies the pending migrations, unless `Database:MigrateOnStartup` is `false` where the schema
is updated otherwise. A new school has no administrator to invite the others, so the API invites the first one by email
while the school has no active administrator: set `FirstAdministrator:Email`, `FirstAdministrator:LastName`,
`FirstAdministrator:FirstName` and, optionally, `FirstAdministrator:PreferredLanguage` (`English` by default). The
administrator sets a password from the invitation; no password is ever kept in the settings. Once the school has an
active administrator, these settings change nothing.

### Demo data

To see every page with something on it while developing, the API can create demo data in a new database: a branch in
Yekaterinburg with a training ground and two cars, a course of category B with six topics, fourteen questions and a
practice test of ten, its staff and three students with contracts, payments and a study group, theory lessons on
Mondays, Wednesdays and Fridays and driving lessons on weekdays for the coming weeks, some of them booked. Each step is
a command of the application sent on behalf of the user who would take it, so the data follows the rules of the school.
It is created only in the Development environment and only when asked for, with one password for every demo user, kept
in the user secrets like any other secret:

```bash
dotnet user-secrets --project src/AutoElite.API set "DemoData:Enabled" "true"
dotnet user-secrets --project src/AutoElite.API set "DemoData:Password" "<8 to 128 characters with a letter and a digit>"
```

The demo users sign in with that password at `admin`, `manager`, `theory` (theory instructor), `driving` (driving
instructor) and `student1` to `student3`, each `@autoelite.example`, a domain reserved for examples, so no email reaches
anybody. The data is created once: a database that has it is left as it is, and a failed step is logged; drop the
database to start again.

### Database migrations

The schema is created by EF Core migrations in `src/AutoElite.Infrastructure/Persistence/Migrations`. The EF Core
tools are pinned in the repository's tool manifest:

```shell
dotnet tool restore
dotnet ef migrations add <Name> --project src/AutoElite.Infrastructure --output-dir Persistence/Migrations
dotnet ef database update --project src/AutoElite.Infrastructure --connection "Host=...;Database=...;Username=...;Password=..."
```

Objects that EF Core does not model, such as exclusion constraints, are SQL scripts in `Persistence/Sql`. A migration
applies the scripts it names with `DatabaseObjects.Create` in `Up` and `DatabaseObjects.Drop` in `Down`; a script that
a migration has applied is never changed, so a change to those objects is a new script in a new migration. The tests
create their databases with the migrations and fail when the model has changes that no migration holds.

## Importing questions

Questions are imported into one course topic from a file, alone or in a ZIP archive with the images it names. An
archive may hold several files of questions, read in the order of their paths, and the images in any folders: a
question names an image by its path, or by the end of its path when no other file ends the same way.

| Format | Extension | Content |
|---|---|---|
| [pdd_russia](https://github.com/etspring/pdd_russia) | `.json` | A list of questions of the official tickets. The `A,B` tickets go to categories A, A1, B, B1 and M, the `C,D` tickets to C, C1, D and D1; a question repeated in several tickets is imported once. Zip the `questions/A_B/tickets` (or `C_D`) folder together with `images` |
| AutoElite | `.json` | `{"format": "autoelite.questions", "version": 1, "questions": [...]}`, each question with `code`, `text`, `explanation`, `categories`, `image` and `options` (`text`, `isCorrect`) |
| Table | `.csv`, `.xlsx` | The first row names the columns: `question`, `correct` (the number of the correct option) and `option1`, `option2`, ... are required; `code`, `explanation`, `categories` and `image` are optional; other columns are ignored. Russian names work too (`Вопрос`, `Правильный ответ`, `Вариант 1`, `Код`, `Пояснение`, `Категории`, `Картинка`). CSV may be UTF-8 or Windows-1251, separated by commas or semicolons, as Excel saves it; of a workbook, the first worksheet is read |

A file is at most 32 MB and an archive at most 512 MB. A question whose code is already in the bank is updated.

## Repository layout

```
src/     production code, one project per layer, the client of the API, its offline mode, the mobile application and
         the website
tests/   one test project per layer (xUnit v3, AwesomeAssertions), plus architecture tests
          that check layer dependencies, code conventions and the copies of the design tokens
design/  Noir Modern, the design system of the mobile application and the website: tokens and fonts
```

## Security

Please report a vulnerability privately, as [SECURITY.md](SECURITY.md) describes, never in a public issue. Dependabot
keeps the packages and the actions up to date, CodeQL analyses the code, NuGet Audit and a check of vulnerable packages
fail the build on a package with a known vulnerability, the actions are pinned by commit and packages come from
nuget.org only.

## License

[GPL-3.0](LICENSE)

The PDF reports use the PT Sans font by ParaType, embedded in the infrastructure assembly under the
[SIL Open Font License 1.1](src/AutoElite.Infrastructure/Reports/Fonts/OFL.txt).
