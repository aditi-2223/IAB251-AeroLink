# AeroLink Project Structure

This document explains what each folder and file in the repository is for, who owns it, and which user story it supports. 

**Owners:** Aditi = Member A (T1, T3, I1, I2, I3), Hetvik = Member B (T2, T4, I4, I5, I6). Shared files are owned by both.

**Status:** ✅ exists now, 🔲 planned for later weeks 

## Repository layout

```
IAB251-AeroLink/
├── README.md                      ✅ Overview, run instructions, team workflow
├── PROJECT_STRUCTURE.md           ✅ Detail structure of files 
├── global.json                    ✅ Pins the .NET 8 SDK for everyone
├── .gitignore                     ✅ Excludes bin/, obj/, *.db
├── AeroLink.sln                   ✅ Solution containing the two projects
├── AeroLink.Web/                  ✅ The Razor Pages application
└── AeroLink.Tests/                ✅ xUnit unit tests
```

## AeroLink.Web (application)

```
AeroLink.Web/
├── Program.cs                     ✅ Startup: services, auth, database seeding, pipeline
├── appsettings.json               ✅ Connection string, HR API base URL
├── Models/                        Data classes (no logic beyond simple helpers)
│   ├── Enums.cs                   🔲 LoadingStatus, BagOutcome, ExceptionStatus        (A, I1)
│   ├── Flight.cs                  🔲 Departure flight and its bags                     (A, I1)
│   ├── Bag.cs                     🔲 Bag on the manifest, IsSpecial helper             (A, I1)
│   └── BagException.cs            🔲 Reported bag problem and its decision             (A, I1)
├── Data/
│   ├── AeroLinkDbContext.cs       🔲 EF Core context, enum-to-string conversions       (A, I1)
│   ├── DbSeeder.cs                🔲 Loads the manifest xlsx into SQLite once          (A, I1)
│   └── SeedData/
│       └── IAB251_Synthetic_Baggage_Manifest.xlsx   🔲 Supplied dataset                (A, I1)
├── Services/                      All business rules live here (this is what tests target)
│   ├── FlightService.cs           🔲 List flights, get manifest                        (A, I1)
│   ├── BagVerificationService.cs  🔲 Validate tag, special-handling ack, confirm load  (A, I2, I3)
│   ├── HrAuthService.cs           🔲 Validate employee via HR API                      (B, T2)
│   ├── ExceptionService.cs        🔲 Report exception, resolve, approve not-to-load    (B, I4, I5)
│   ├── LoadingStatusService.cs    🔲 Expected/loaded/outstanding counts, readiness     (B, T4)
│   └── LoadingClosureService.cs   🔲 CanClose check, close, final summary              (B, I6)
└── Pages/                         Razor Pages: thin, call services only
    ├── Index.cshtml               🔲 Main screen with three module tiles               (A, T1)
    ├── Shared/
    │   ├── _Layout.cshtml         🔲 Navbar, sign-in state, shared styling             (A, T1)
    │   └── _ValidationScriptsPartial.cshtml  ✅ Template default
    ├── Account/
    │   ├── Login.cshtml(.cs)      🔲 Employee ID + email form, HR validation           (B, T2)
    │   ├── Logout.cshtml(.cs)     🔲 Clears session, returns to main screen            (A, T3)
    │   └── AccessDenied.cshtml    🔲 Shown when a role lacks permission                (B, T2)
    └── Baggage/                   Whole folder requires sign-in (see Program.cs)
        ├── Flights.cshtml(.cs)    🔲 Choose a departure flight                         (A, I1)
        ├── Manifest.cshtml(.cs)   🔲 Expected bags, type, status                       (A, I1)
        ├── Loading.cshtml(.cs)    🔲 Enter tag, special-handling ack, report problem   (A, I2/I3 + B, I4)
        ├── Exceptions.cshtml(.cs) 🔲 Supervisor queue, resolve or approve not-to-load  (B, I5)
        ├── Status.cshtml(.cs)     🔲 Live loading status report                        (B, T4)
        ├── Close.cshtml(.cs)      🔲 Supervisor closes loading                         (B, I6)
        └── Summary.cshtml(.cs)    🔲 Final loading summary, read-only                  (B, I6)
```

The template also created `Pages/Privacy.cshtml`, `Pages/Error.cshtml` and `wwwroot/`. Leave `wwwroot/` (Bootstrap, CSS) as is. Remove `Privacy` if it isn't used.

## AeroLink.Tests (unit tests)

```
AeroLink.Tests/
├── HrAuthServiceTests.cs               🔲 Mocked HTTP handler cases                    (B, T2)
├── FlightServiceTests.cs               🔲 Manifest and flight lookups                  (A, I1)
├── BagVerificationServiceTests.cs      🔲 Valid, invalid, other-flight, duplicate      (A, I2) *individual test story*
├── ExceptionServiceTests.cs            🔲 Report, resolve, approve with reason         (B, I4/I5)
├── LoadingStatusServiceTests.cs        🔲 Counts and readiness                         (B, T4)
└── LoadingClosureServiceTests.cs       🔲 Every closure condition                      (B, I6) *individual test story*
```

One test file per service, named `<Service>Tests.cs`. Tests use an in-memory SQLite database, and a fake `HttpMessageHandler` for the HR API.

## Data model summary

| Table | Key columns | Notes |
|---|---|---|
| Flights | FlightId, FlightNumber, ScheduledDepartureUtc, LoadingStatus | Status: NotStarted, InProgress, Completed |
| Bags | BagId, FlightId, Tag (unique), HandlingType, HandlingInstruction, Outcome | Outcome: Pending, Loaded, ApprovedNotToLoad. Any type other than Standard is special. |
| Exceptions | ExceptionId, BagId, FlightId, Category, Description, Status, DecisionReason, ReportedAtUtc | Status: Open, Resolved |

Employees are not stored locally. They are validated live against the HR API, and the role comes from the HR `JobTitle` (`Baggage Handler` or `BG_Supervisor`).

## Page routes (shared contract)

The selected flight travels in the query string so pages stay stateless and independent.

| Route | Purpose | Roles |
|---|---|---|
| `/` | Main screen | Anyone |
| `/Account/Login` | Sign in | Anyone |
| `/Baggage/Flights` | Choose flight | Handler, Supervisor |
| `/Baggage/Manifest?flightId=101` | Manifest | Handler, Supervisor |
| `/Baggage/Loading?flightId=101` | Verify and load, report problem | Handler, Supervisor |
| `/Baggage/Status?flightId=101` | Live status report | Handler, Supervisor |
| `/Baggage/Exceptions?flightId=101` | Decide exceptions | Supervisor only |
| `/Baggage/Close?flightId=101` | Close loading | Supervisor only |
| `/Baggage/Summary?flightId=101` | Final summary | Handler, Supervisor |

## How a request flows

```
Browser → Razor Page (PageModel) → Service → DbContext → SQLite
                                     ↓
                            HrAuthService → HR API (separate app, https://localhost:5001)
```

Pages do no business logic. They call one service method, then show the result or an error message. All rules (duplicate tags, special-handling acknowledgement, closure checks) live in `Services/` so they can be unit tested without the UI.

## Coding conventions

- Namespaces follow folders: `AeroLink.Web.Models`, `AeroLink.Web.Services`, `AeroLink.Web.Pages.Baggage`.
- Every public class and method in `Models/`, `Data/` and `Services/` has an XML doc comment covering purpose, parameters, return value and error behaviour.
- `PascalCase` for types and methods, `camelCase` for parameters and locals, `_camelCase` for private fields.
- Interfaces are named `IFlightService`, `IHrAuthService` and so on, and services are registered in `Program.cs`.
- One class per file, and the file name matches the class name.

## Branches and commits

- `main` is protected and always runs.
- Branch names: `feature/T1-main-screen`, `feature/I1-data-model`, `feature/T2-hr-login`, `fix/<short-description>`, `docs/<short-description>`.
- Every commit and PR mentions its Azure Boards work item ID.
- The other team member reviews every PR before it merges.

## Local development notes

- Start the HR API first (`https://localhost:5001`), then AeroLink.
- `AeroLink.db` is created and seeded on first run and is not committed. If the model changes, delete the file and restart.
- The seeder skips exception #6 in the manifest (its bag is on flight 102, not 101) and prints a warning.