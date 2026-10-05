# AeroLink: Departure Baggage Loading Verification and Exception Management

IAB251 Assessment 2 prototype. A Razor Pages web app that lets Baggage Handlers verify bags against a flight manifest and lets Baggage Supervisors resolve exceptions and close loading.

## Team

| Member | Stories |
|---|---|
| Aditi Vansjaliya | T1, T3, I1, I2|
| Hetvik Desai | T2, T4, I3, I4|

## Tech stack

- ASP.NET Core Razor Pages (.NET 8)
- Entity Framework Core with SQLite
- xUnit for unit tests
- External HR system consumed through its REST API
- Azure DevOps Boards for planning, GitHub for version control

## Prerequisites

- .NET 8 SDK (`dotnet --list-sdks` should list an 8.x version)
- The supplied HR System API project (`HRSystem`)

## How to run

1. Start the HR API first:
```bash
   cd HRSystem
   dotnet run
```
   It listens on `https://localhost:5001`.

2. In a second terminal, start AeroLink:
```bash
   cd AeroLink.Web
   dotnet run
```
The console prints the URL to open. The database (`AeroLink.db`) is created and seeded from the manifest on first run.

If you change the data model, delete `AeroLink.db` and restart so it is rebuilt.

## Demo accounts (from the HR system)

| Employee ID | Email | Role |
|---|---|---|
| 100101 | s.mitchell@company.com | Baggage Handler |
| 300100 | p.sharma@company.com | Baggage Handler |
| 200100 | j.cooper@company.com | Baggage Supervisor |

## Running the tests

```bash
dotnet test
```

## Project structure

```
AeroLink.Web/     Razor Pages app (Models, Data, Services, Pages)
AeroLink.Tests/   xUnit unit tests
```

## Team workflow

- `main` is always in a working state. Nobody pushes to it directly.
- Each user story or task gets its own branch, e.g. `feature/I1-data-model`.
- Changes reach `main` through a pull request reviewed by the other team member.
- Commit messages and PRs reference the Azure Boards work item ID.