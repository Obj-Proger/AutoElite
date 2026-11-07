# AutoElite

[![CI](https://github.com/Obj-Proger/AutoElite/actions/workflows/ci.yml/badge.svg)](https://github.com/Obj-Proger/AutoElite/actions/workflows/ci.yml)

Management system for driving schools: the full student lifecycle from the first enquiry to
graduation, in one self-hosted application.

> **Status: early development.** The domain model and the application layer are in place;
> persistence is being written; the HTTP API and the clients are not written yet. Nothing here is
> ready for production use.

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
| `AutoElite.Infrastructure` | Persistence (EF Core, PostgreSQL), identity and external services *(in progress)* |
| `AutoElite.API` | Versioned public HTTP API *(not implemented yet)* |

Expected failures are returned as `Result` values with typed errors rather than thrown; validation
errors are reported per field. Each school deploys its own instance (single-tenant).

Planned stack: .NET 10, ASP.NET Core, EF Core with PostgreSQL, Blazor (web), .NET MAUI (mobile).

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

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

## Repository layout

```
src/     production code, one project per layer
tests/   one test project per layer (xUnit v3, AwesomeAssertions), plus architecture tests
          that check layer dependencies and code conventions
```

## License

[GPL-3.0](LICENSE)
