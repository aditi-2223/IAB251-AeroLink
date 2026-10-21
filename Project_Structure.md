# AeroLink Project Structure

This document explains what each folder and file is for, who owns it, and which user story it supports.

**Owners:** Aditi = Member A (T1, T3, I1, I3). Hetvik = Member B (T2, I2, I4).

**Status:** ✅ done, 🔄 in progress, 🔲 still to do

**Scope note:** Stories I5 (supervisor decision) and I6 (close loading) are out of scope for Assessment 2 (confirmed with the tutor). The workflow ends after reporting an exception (I4) and viewing the status report (T4).

## The workflow

```
Sign in → Main screen → Select flight → View manifest → Verify and load a bag
        → Acknowledge special handling → Report an exception → View status report → Log out
```

## Repository layout

```
IAB251-AeroLink/
├── README.md                ✅ Overview and how to run
├── PROJECT_STRUCTURE.md     ✅ This file
├── sprint_plan.md           ✅ Sprint plan and MVP
├── global.json              ✅ Pins the .NET 8 SDK
├── .gitignore               ✅ Excludes bin/, obj/, *.db
├── AeroLink.sln             ✅ Solution file
├── AeroLink.Web/            ✅ The Razor Pages application
└── AeroLink.Tests/          ✅ xUnit unit tests
```

## AeroLink.Web (the application)

```
AeroLink.Web/
├── Program.cs                      ✅ Startup: services, cookie login, seeding, pipeline (A)
├── appsettings.json                ✅ Database connection string (A)
│
├── Models/                         Plain data classes
│   ├── Enums.cs                    ✅ Status and outcome enums               (A, I1)
│   ├── Flight.cs                   ✅ A departure flight and its bags        (A, I1)
│   ├── Bag.cs                      ✅ A bag on the manifest                  (A, I1)
│   └── BagException.cs             ✅ A reported bag problem                 (A, I1)
│
├── Data/
│   ├── AeroLinkDbContext.cs        ✅ EF Core database context               (A, I1)
│   ├── DbSeeder.cs                 ✅ Loads the manifest xlsx into SQLite    (A, I1)
│   └── SeedData/
│       └── IAB251_Synthetic_Baggage_Manifest.xlsx   ✅ Supplied dataset      (A, I1)
│
├── Services/                       Business rules (this is what the tests target)
│   ├── FlightService.cs            ✅ List flights, get a manifest           (A, I1)
│   ├── SpecialHandlingService.cs   🔲 Is a bag special, its instruction, acknowledgement rule (A, I3)
│   ├── BagVerificationService.cs   🔲 Check a bag tag and confirm loading    (B, I2)
│   ├── HrAuthService.cs            🔲 Check an employee with the HR API      (B, T2)
│   ├── ExceptionService.cs         🔲 Report a bag exception                 (B, I4)
│   
│
└── Pages/                          Razor Pages (thin, they just call services)
    ├── Index.cshtml                ✅ Main screen with module tiles          (A, T1)
    ├── Shared/
    │   └── _Layout.cshtml          ✅ Navbar with sign-in state and log out  (A, T1 + T3)
    ├── Account/
    │   ├── Login.cshtml(.cs)       ✅ TEMPORARY sign-in, B replaces with HR login (A temporary, B T2)
    │   └── Logout.cshtml(.cs)      🔄 Signs out and goes to login            (A, T3)
    └── Baggage/                    Everything in this folder needs sign-in
        ├── Flights.cshtml(.cs)     ✅ Choose a flight                        (A, I1)
        ├── Manifest.cshtml(.cs)    ✅ Expected bags (I3 adds category and instruction) (A, I1 + I3)
        ├── Loading.cshtml(.cs)     🔲 Enter tag and confirm (B, I2). Special-handling panel (A, I3). 
```

The template also created `Pages/Privacy.cshtml`, `Pages/Error.cshtml` and `wwwroot/`. Leave `wwwroot/` alone. `Privacy` is no longer linked from the navbar and can be deleted.

`SpecialHandlingService` is a separate class so I3 (A) and I2 (B) do not edit the same file. B's `BagVerificationService` calls it before confirming a special bag.

## AeroLink.Tests (unit tests)

```
AeroLink.Tests/
├── FlightServiceTests.cs            🔲 Flight and manifest lookups                (A, I1)
├── SpecialHandlingServiceTests.cs   🔲 Standard vs special bags, acknowledgement  (A, I3) individual test story
├── BagVerificationServiceTests.cs   🔲 Valid, invalid, duplicate, wrong flight    (B, I2) individual test story
├── ExceptionServiceTests.cs         🔲 Reporting an exception                     (B, I4)
├── HrAuthServiceTests.cs            🔲 HR API success and failure cases           (B, T2)
```

One test file per service, named `<Service>Tests.cs`. Tests cover typical cases and boundary cases, and use mocks or an in-memory database where a service depends on something else.

## Data model

| Table | Main columns | Notes |
|---|---|---|
| Flights | FlightId, FlightNumber, ScheduledDepartureUtc, LoadingStatus | |
| Bags | BagId, FlightId, Tag (unique), HandlingType, HandlingInstruction, Outcome | Any handling type other than Standard is "special" |
| Exceptions | ExceptionId, BagId, FlightId, Category, Description, Status, ReportedAtUtc | |

Some enum values and columns exist from the original design but are **not used** in this prototype because I5 and I6 were removed: the `Completed` flight status, the `ApprovedNotToLoad` bag outcome, the `Resolved` exception status and `DecisionReason`.

Employees are not stored locally. They are checked against the HR API.

## Page routes

All pages need sign-in except `/Account/Login`. The selected flight is passed in the query string.

| Route | Purpose |
|---|---|
| `/` | Main screen |
| `/Account/Login` | Sign in |
| `/Account/Logout` | Sign out (POST only) |
| `/Baggage/Flights` | Choose a flight |
| `/Baggage/Manifest?flightId=101` | View the manifest |
| `/Baggage/Loading?flightId=101` | Verify and load bags, report a problem |
| `/Baggage/Status?flightId=101` | Loading status report |

## How a request flows

```
Browser → Razor Page → Service → DbContext → SQLite
                         ↓
                HrAuthService → HR API (separate app, https://localhost:5001)
```

Pages contain no business rules. They call a service and show the result or an error message. Rules such as duplicate tags and special-handling acknowledgement live in `Services/` so they can be unit tested without the UI.

## Coding conventions

- Namespaces follow folders, for example `AeroLink.Web.Services`.
- Every public member in `Models/`, `Data/` and `Services/` has an XML doc comment (purpose, parameters, return value, errors, pre and post-conditions). A one-line summary is enough for plain data properties.
- `PascalCase` for types and methods, `camelCase` for parameters and locals, `_camelCase` for private fields.
- Interfaces start with `I` (for example `IFlightService`) and are registered in `Program.cs`.
- One class per file, and the file name matches the class name.

## Branches and commits

- `main` should always build and run.
- One branch per story, for example `feature/I3-special-handling` or `feature/I2-bag-verification`. Other prefixes: `fix/` and `docs/`.
- Every commit message names its story, for example `Add logout handler (T3)`.
- Work is merged into `main` through a pull request, and the Azure DevOps task moves to Closed once merged.

## Running locally

- Run the app with `dotnet run --project AeroLink.Web`.
- Run the tests with `dotnet test`.
- When T2 is done, start the HR API first (`https://localhost:5001`), then AeroLink.
- `AeroLink.db` is created and seeded on first run and is not committed. If the model changes, delete the file and restart.
- The seeder skips exception #6 in the manifest because its bag belongs to flight 102, not 101, and prints a warning.