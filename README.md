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
| CRM | Leads from the public form, statuses, notes, a limit of leads in progress per manager, conversion into a contract |
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

### Quick start

The quickest way to run the API and the website on Windows, with PostgreSQL in Docker and the [demo data](#demo-data),
needs the .NET 10 SDK, Docker Desktop and PowerShell:

```powershell
.\scripts\Initialize-DevEnvironment.ps1   # once: the database, the keys and the settings; asks for the demo password
.\scripts\Start-DevEnvironment.ps1        # each time: the database, then the API and the website in two windows
.\scripts\Reset-DevDatabase.ps1           # to start the demo again: an empty database, filled on the next start
```

The first script writes a random password of the database to `.env`, which git ignores, starts PostgreSQL 18 from
`compose.dev.yaml` at `localhost:5433`, reachable from this machine only, and keeps everything else in the user secrets
of the API: random keys of the tokens and of the chats, the folders of the files and of the emails under
`%LOCALAPPDATA%\AutoElite`, the address of the website and the demo data with the password it asks for. It keeps what is
already set, so it may be run again, except the connection string, which it points to the database in Docker. The API
then answers at `http://localhost:5080/scalar` and the website at `http://localhost:5173`; emails, such as invitations,
are `.eml` files in `%LOCALAPPDATA%\AutoElite\mail`, which any mail program opens. `docker compose -f compose.dev.yaml
down` stops PostgreSQL, and with `-v` also deletes its data, so that the next start creates the demo data again. On
Linux and macOS the scripts run in PowerShell 7 as well; start the API and the website with `dotnet run --project
src/AutoElite.API` and `dotnet run --project src/AutoElite.Web`.

Windows PowerShell runs no script of a file until it is allowed once for the user: `Set-ExecutionPolicy -Scope
CurrentUser RemoteSigned` lets local scripts run and still stops unsigned ones downloaded from the internet.

With a PostgreSQL server that already runs, such as one on another machine or behind a Docker context of a remote host,
pass `-ExistingDatabase` to the scripts: the first asks for the connection string without showing it, keeps it in the
user secrets and starts nothing in Docker. The API creates the database when it first starts, if the user of the
connection string may create databases; otherwise create an empty one first. `Reset-DevDatabase.ps1 -ExistingDatabase`
drops the database of the connection string and creates it empty, owned by its user, with psql or, without it, psql in
Docker; it asks to type the name of the database first, and the password goes to psql through the environment of the
script only.

### Running the API

Without the scripts above, the API needs PostgreSQL and a few settings and secrets of the machine, which stay out of
the repository in user secrets. Generate each key once (in PowerShell: `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`):

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
applications of the school register it with `services.AddAutoEliteClient(apiAddress)`: `AuthSessionManager` signs in and
out, and the requests of `AutoEliteApiClient` carry the access token, which is renewed with the refresh token when it
expires or the API rejects it. A file is attached with `AttachDocumentAsync`, which sends the multipart form as parts,
since the model that Kiota makes of the form cannot be written as one. An application keeps the session in its own
`IAuthSessionStore`, such as secure storage on a device.

### The mobile application

`src/AutoElite.Mobile` is the .NET MAUI application of students, driving instructors and theory instructors, for Android
and Windows. Building it, and so the whole solution, needs the MAUI workload: `dotnet workload install maui`, or the
*.NET Multi-platform App UI development* workload of Visual Studio, which also installs the Android SDK. The application
keeps the session in the secure storage of the device. On the first start it asks for the address of the school's API,
which must use HTTPS; a debug build also takes HTTP and offers the API of the development machine:
`http://localhost:5080` on Windows, `http://10.0.2.2:5080` from the Android emulator. Students, instructors, managers
and administrators sign in; the rest of the work of the staff is on the website. Above every tab are its title, the
bell with the count of unread notifications, up to 99+, and the avatar, which opens the profile. The tabs follow the
width of the window on every platform: a bar at the bottom of a phone, a rail of icons at the left of a narrow window
and a menu with captions at the left of one from 1000 wide. They lead to today, the schedule, the studies of a student
(their practice tests and contracts), the students and the leads of a manager, with the count of new leads, and the
chats, with the count of unread messages. The schedule switches between a day, chosen in the strip of its week, and the
week. A lesson is a card with the icon of its kind, what it is, who and where or its state, and its start and length; a
free slot is a dashed card, and so is the free time of an instructor between the cards of a day. The week is a list
under the names of its days with lessons or, on a wide window, a grid of hours; arrows or a swipe move the schedule and
a link brings it back to today. It shows the lessons of each role of the user, a student also the free slots they can
book, and tapping a lesson shows what the user can do with it, such as booking, cancelling, completing or marking the
attendance. A student sees on the tab of today their next lesson in a highlighted card, with the way to its
ground, the hours they have driven and what they owe, and their following lessons, books a free practice slot of one of
their contracts and cancels a booking, with or without a network, and takes practice tests and theory exams with the
timer and a review of their mistakes, which need a network; rates a completed lesson; and follows the progress of their
training with its payments; pulling the page down sends the waiting actions and reads the lessons again. A driving
instructor offers a practice slot by tapping free time on the schedule, with its length, vehicle and training ground,
repeats the slots of a week on up to four next weeks, leaving out those that would overlap, withdraws a slot with a
reason, which needs a network, sees their slots of the next two weeks, cancels a booking before the lesson and, once it
has started, completes it with the hours, the grades of the student's skills and a note, or records that the student did
not come, also without a network. A theory instructor schedules a lesson of one of their groups on a topic of its course
by tapping free time on the schedule, moves or cancels a lesson before it starts, with the students notified, which
needs a network, sees their theory lessons and marks the attendance of a lesson that has started for the whole group at
once, in a classroom without a network too. A manager or an administrator sees the slots, theory lessons and exams of
every instructor, narrowed to one of them if they choose, books a student found by a part of the name, the email address
or the phone number on a free slot for their contract that fits the vehicle, which tells the student, and cancels a
booking; in the tab of students they find a student and open their card with each contract, what was paid and what is
owed, the progress and the payments, record a payment, received at once or waiting for its money, and confirm a waiting
one. In the tab of leads a manager sees the leads, the new ones and those they work on with how many of them their limit
allows, takes a new lead on, which makes them its manager within that limit, calls or writes to the person, writes
notes, marks a lead lost with the reason and opens it again; the contract of a lead is drawn up on the website. An
administrator sets the limit of each manager, ten leads in progress at first, on the card of the user on the website,
and may give a manager leads beyond it. In the tab of studies, a theory instructor finds the questions the school wrote
for the practice tests, writes a new one and corrects one: its text and illustration, the topic of a course, the licence
categories, two to six options with the correct one marked and an explanation; a question is taken out of new tests
rather than deleted. Every user reads their notifications, whose number on the bell changes at once through the SignalR
hub, and their chats, where new messages appear at once and a message written without a network is sent later. The chats
are found by a search; on a wide window a chat opens next to their list, with the person or the group above its
messages, and on a phone on a page of its own. A personal chat starts, by the + button, from the people the user studies
or teaches with (`GET /api/v1/chats/contacts`): a student's instructors, an instructor's students. A message carries up
to ten files, which need a network and open in another application from the cache of the device, emptied when the user
signs out; the user deletes their own messages, turns the notifications of a chat off and leaves a group chat. In the
profile the user corrects their name, phone and the language of their notifications and emails, sets or removes their
photo, changes their password, which signs out their other devices, and asks for a new email address, which changes once
they follow the link sent to it. An instructor opens the progress of a student they teach from a lesson or a personal
chat (`GET /api/v1/students/{id}/progress`, without the money of the contracts): the practice hours, the driving
lessons, the attendance of the group, the tests and the exams, and for a driving instructor the lessons with them, with
the hours, the grade and the note; they write to or call the student from there. The schedule shows the school exams
too: a student's own with their result, the practice exams of a driving instructor's branch and the exams of a theory
instructor's groups. An exam opens with the current members of its group (`members` of `GET /api/v1/exams/{id}`) and
their results; once a practice exam has begun, a driving instructor records each result, and a recorded result is
corrected with a reason, a practice one by a driving instructor and a theory one by the instructor responsible for the
group. A member of the staff sees on the tab of today how many of their tasks are open and overdue; the tasks open by
their deadline, and the assignee starts a task, marks it done, after which a recurring one comes back, and opens it
again. In the tab of studies, a student opens the documents of their contracts and adds their own, such as a scan of
their passport, with a description, and deletes those they added; an opened file is kept in the cache of the device
until they sign out. The texts of the application are in English, with a Russian translation, in `Resources/Strings`;
the device's language chooses between them unless the user chooses one in the profile, where they also change the school
and sign out. The colors follow the light or dark theme of the device, or the theme the user chooses in the profile,
which shows at once.

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

To see every part of the system in use while developing, the API can create demo data in a new database: a school three
weeks into the training of a group. A branch in Yekaterinburg with a training ground and two cars, a course of category
B with six topics, fourteen questions and a practice test of ten, its staff and three students with contracts, payments,
scanned contracts and a study group. Behind them: theory lessons on Mondays, Wednesdays and Fridays with the attendance
marked, driving lessons completed with grades and notes, one missed, rated by the first student, practice tests passed
and failed, a theory exam three days ago with its results, personal chats with a file, a medical certificate among the
documents of the first student, the tasks the manager gave the instructors, one overdue, one started, one recurring and
one done, and the leads of the last days, new from the website and the phone, one the manager works on and one lost,
with a payment of the second student that waits for its money. Today: two driving lessons that have begun and wait for
the instructor, and a practice exam that began two hours ago, with one result recorded. Ahead: theory lessons for three
weeks, driving lessons on weekdays for two, some of them booked, and a theory exam. Each step is a command of the
application sent on behalf of the user who would take it, so the data follows the rules of the school; the past is
created with the clock of the host turned back to when it happened, and the clock is the system clock again before the
API answers. It is created only in the Development environment and only when asked for, with one password for every demo
user, kept in the user secrets like any other secret:

```bash
dotnet user-secrets --project src/AutoElite.API set "DemoData:Enabled" "true"
dotnet user-secrets --project src/AutoElite.API set "DemoData:Password" "<8 to 128 characters with a letter and a digit>"
```

The demo users sign in with that password at `admin`, `manager`, `theory` (theory instructor), `driving` (driving
instructor) and `student1` to `student3`, each `@autoelite.example`, a domain reserved for examples, so no email reaches
anybody. The data is created once: a database that has it is left as it is, and a failed step is logged; drop the
database to start again (`scripts/Reset-DevDatabase.ps1`), which also moves the past and the future of the demo to the
new day.

### A tour of the demo

With the demo data, sign in as each kind of user to see what they do:

- **Administrator** (`admin@autoelite.example`, on the website): the users of the school, finding them, inviting a new
  one and opening the invitation from the mail folder to set their password, correcting a user, granting a role, moving
  a user between branches, setting how many leads a manager may work on at a time, deactivating and reactivating a user.
  The API reference at `/scalar` and the background jobs at `/jobs` show what the website does not yet.
- **Manager** (`manager@autoelite.example`, on the website): the users of their branch only; they invite students. In
  the application: the schedule of every instructor, narrowed to one, booking a student on a free slot; the tab of
  students with the card of a student, recording a payment and confirming the waiting one of `student2`; the tab of
  leads with the number in progress of the limit, taking a new one on, a note, marking one lost.
- **Student** (`student1@autoelite.example`, in the mobile application, at `http://10.0.2.2:5080` from the Android
  emulator or `http://localhost:5080` on Windows): the next lesson on today and the week on the schedule,
  with the theory exam behind and the one ahead; booking and cancelling a slot and rating a lesson; the practice tests
  with the attempts behind and the exam result; the contract with its payments and progress and the documents, with the
  scanned contract and the medical certificate; the chat with the driving instructor, with the route of a lesson, and
  the notifications; the profile with the photo, the password and the email. Turned offline, the application shows what
  it last read and sends a booking once the network is back. `student2` has paid part of the price, missed a lesson and
  failed the theory exam; `student3` has paid nothing and trains on the automatic car.
- **Driving instructor** (`driving@autoelite.example`, in the application): the schedule of slots with the bookings of
  the students; offering a slot by tapping free time, repeating the week, withdrawing a slot; a lesson completed with
  the grades of the skills or marked missed; the progress of a student from a booked lesson or the chat; the practice
  exam of today, recording the results of the second and third students; the tasks on today, one overdue.
- **Theory instructor** (`theory@autoelite.example`, in the application): the lessons of the group, scheduling, moving
  and cancelling one, the attendance marked once a lesson has started; the exams of the group, correcting a theory
  result; the questions of the tests in the tab of studies; the chat with the second student; the tasks.

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
[SIL Open Font License 1.1](src/AutoElite.Infrastructure/Reports/Fonts/OFL.txt). The application and the website use
Inter and Unbounded under the same license ([`design/fonts`](design/fonts)), and the icons of the application are drawn
after Lucide, under the [ISC License](design/icons/Lucide-LICENSE.txt).
