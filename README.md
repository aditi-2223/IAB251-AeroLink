# AeroLink: Departure Baggage Loading Verification

IAB251 Assessment 2 prototype. A Razor Pages web app that lets Baggage Handlers verify bags against a flight manifest, follow special-handling instructions, and report problems.

## Team

| Member | Stories |
|---|---|
| Aditi Vansjaliya | T1, T3, I1, I3 |
| Hetvik Desai | T2, I2, I4 |

The team is two people, so the scope is T1, T2, T3, I1, I2, I3 and I4 (confirmed with the tutor). Stories I5, I6 and T4 are not part of this assessment, so the workflow ends after reporting an exception.

## Workflow

```
Sign in → Main screen → Select flight → View manifest → Verify and load a bag
        → Acknowledge special handling → Report an exception → Log out
```

## Current status

| Story | What it does | Owner | Status |
|---|---|---|---|
| T1 | Main screen, navigation, sign-in state | Aditi | ✅ |
| I1 | View expected bags for a flight | Aditi | ✅ |
| T3 | Log out | Aditi | 🔄 |
| I3 | Special-handling acknowledgement | Aditi | 🔲 |
| T2 | Sign in using the HR API | Hetvik | 🔲 |
| I2 | Verify and confirm a bag | Hetvik | 🔄 |
| I4 | Report a bag problem | Hetvik | 🔲 |

Until T2 is finished, sign-in is a **temporary** page that accepts any name, and the HR API is not needed.

## Tech stack

- ASP.NET Core Razor Pages (.NET 8)
- Entity Framework Core with SQLite
- ClosedXML to read the supplied manifest spreadsheet
- xUnit for unit tests
- External HR system consumed through its REST API
- Azure DevOps Boards for planning, GitHub for version control

## Prerequisites

- .NET 8 SDK (`dotnet --list-sdks` should list an 8.x version). `global.json` pins it.
- The supplied HR System API project (`HRSystem`), needed once T2 is merged.

## How to run

From the repository root:

```bash
dotnet build
dotnet run --project AeroLink.Web
```

The console prints the `https://localhost:xxxx` address to open. The database (`AeroLink.db`) is created and seeded from the manifest on the first run.

If you change the data model, delete `AeroLink.db` and restart so it is rebuilt.

**Once the real login (T2) is merged**, start the HR API first in a separate terminal:

```bash
dotnet run --project HRSystem
```

It listens on `https://localhost:5001`. Then start AeroLink as above.

**Tip:** the login is a browser cookie that lasts for the browser session. To see the sign-in page again, use the Log out button or a private window.

## Demo accounts (from the HR system)

Used once T2 is merged.

| Employee ID | Email | Role |
|---|---|---|
| 100101 | s.mitchell@company.com | Baggage Handler |
| 300100 | p.sharma@company.com | Baggage Handler |
| 200100 | j.cooper@company.com | Baggage Supervisor |

The Baggage Supervisor account can sign in, but this prototype has no supervisor-only features.

## Running the tests

```bash
dotnet test
```

## Project structure

```
AeroLink.Web/     Razor Pages app (Models, Data, Services, Pages)
AeroLink.Tests/   xUnit unit tests
```

[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for every file and its owner, and [sprint_plan.md](sprint_plan.md) for the sprint plan and MVP.

## Team workflow

- `main` should always build and run.
- Each user story or task gets its own branch, for example `feature/I3-special-handling`.
- Changes reach `main` through a pull request. The other team member reviews where possible.
- Commit messages and pull requests reference the story, for example `Add logout handler (T3)`.
- The Azure DevOps task moves to Active when work starts and Closed when the pull request is merged.
