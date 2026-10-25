# AeroLink Project Structure

This document explains what each folder and file is for, who owns it, and which user story it supports.

**Owners:** Aditi = Member A (T1, T3, I1, I3). Hetvik = Member B (T2, I2, I4).

**Status:** ✅ done, 🔄 in progress, 🔲 still to do

**Scope note:** The team is two people, so the scope is T1, T2, T3, I1, I2, I3 and I4. Stories I5, I6 and T4 are not part of this assessment. The workflow ends after reporting an exception (I4).

**Current status:** every file below is complete and merged.

## The workflow

```
Sign in → Main screen → Select flight → View manifest → Verify and load a bag
        → Acknowledge special handling → Report an exception → Log out
```

## Repository layout

```
IAB251-AeroLink/
├── README.md                ✅ Overview and how to run (HR system first, then AeroLink)
├── PROJECT_STRUCTURE.md     ✅ Current file
├── sprint_plan.md           ✅ Sprint plan, MVP and reflection log
├── global.json              ✅ Pins the .NET 8 SDK
├── .gitignore               ✅ Excludes bin/, obj/, *.db
├── AeroLink.sln             ✅ Solution file
├── AeroLink.Web/            ✅ The Razor Pages application
└── AeroLink.Tests/          ✅ xUnit unit tests
```

The supplied `HRSystem` API project is a separate application. It is not part of this repository's code and must be running (`https://localhost:5001`) before AeroLink is started, so that sign-in works.

## AeroLink.Web (the application)

```
AeroLink.Web/
├── Program.cs                      ✅ Startup: services, cookie login, HR API client, seeding, pipeline (A + B, T2 adds HR client registration)
├── appsettings.json                ✅ Database connection string and HR API address (A, B)
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
│   ├── SpecialHandlingService.cs   ✅ Is a bag special, its instruction, acknowledgement rule (A, I3)
│   ├── BagVerificationService.cs   ✅ Check a bag tag and confirm loading    (B, I2)
│   ├── HrAuthService.cs            ✅ Check an employee with the HR API      (B, T2)
│   └── ExceptionService.cs         ✅ Report a bag exception                 (B, I4)
│
└── Pages/                          Razor Pages (they just call services)
    ├── Index.cshtml                ✅ Main screen with module tiles          (A, T1)
    ├── Shared/
    │   └── _Layout.cshtml          ✅ Navbar with sign-in state and log out  (A, T1 + T3)
    ├── Account/
    │   ├── Login.cshtml(.cs)       ✅ HR login: email and Employee ID checked through HrAuthService, shows errors (B, T2; replaced A's temporary sign-in from T1)
    │   └── Logout.cshtml(.cs)      ✅ Signs out and goes to login            (A, T3)
    └── Baggage/                    Everything in this folder needs sign-in
        ├── Flights.cshtml(.cs)     ✅ Choose a flight                        (A, I1)
        ├── Manifest.cshtml(.cs)    ✅ Expected bags with special-handling category and instruction (A, I1 + I3)
        └── Loading.cshtml(.cs)     ✅ Enter tag and confirm (B, I2), special-handling panel and acknowledgement (A, I3), report a problem (B, I4)
```

The template also created `Pages/Error.cshtml` and `wwwroot/`.

## AeroLink.Tests (unit tests)

```
AeroLink.Tests/
├── FlightServiceTests.cs            ✅ Flight and manifest lookups                (A, I1)
├── SpecialHandlingServiceTests.cs   ✅ Standard vs special bags, acknowledgement  (A, I3) individual test story
├── BagVerificationServiceTests.cs   ✅ Valid, invalid, duplicate, wrong flight    (B, I2) individual test story
├── ExceptionServiceTests.cs         ✅ Reporting an exception                     (B, I4)
└── HrAuthServiceTests.cs            ✅ HR API success and failure cases (mocked)  (B, T2)
```

One test file per service, named `<Service>Tests.cs`. Tests cover typical cases and boundary cases, and use mocks or an in-memory database where a service depends on something else. The HR API is mocked, so the tests run without the HR system.

## Data model

| Table | Main columns | Notes |
|---|---|---|
| Flights | FlightId, FlightNumber, ScheduledDepartureUtc, LoadingStatus | |
| Bags | BagId, FlightId, Tag (unique), HandlingType, HandlingInstruction, Outcome | Any handling type other than Standard is "special" |
| Exceptions | ExceptionId, BagId, FlightId, Category, Description, Status, ReportedAtUtc | New exceptions are stored as Open |

Employees are not stored locally. They are checked against the HR API.

## Page routes

All pages need sign-in except `/Account/Login`. The selected flight is passed in the query string.

| Route | Purpose |
|---|---|
| `/` | Main screen |
| `/Account/Login` | Sign in (validated against the HR API) |
| `/Account/Logout` | Sign out (POST only) |
| `/Baggage/Flights` | Choose a flight |
| `/Baggage/Manifest?flightId=101` | View the manifest |
| `/Baggage/Loading?flightId=101` | Verify and load bags, acknowledge special handling, report a problem |

## How a request flows

```
Browser → Razor Page → Service → DbContext → SQLite
                         ↓
                HrAuthService → HR API (separate app, https://localhost:5001)
```

Pages contain no business rules. They call a service and show the result or an error message. Rules such as duplicate tags and special-handling acknowledgement live in `Services/` so they can be unit tested without the UI.

## Coding conventions

- Namespaces follow folders, for example `AeroLink.Web.Services`.
- Every public member in `Models/`, `Data/` and `Services/` has an XML doc comment. 
- Interfaces start with `I` (for example `ISpecialHandlingService.cs`) and are registered in `Program.cs`.
- One class per file, and the file name matches the class name.

## Branches and commits

- `main` should always build and run.
- One branch per story, for example `feature/I3-special-handling` or `feature/I2-bag-verification`. O
- Every commit message names its story, for example `Add logout handler (T3)`.
- Work is merged into `main` through a pull request, and the Azure DevOps task moves to Closed once merged.

## Running locally

- **Start the HR system first** (`dotnet run --project HRSystem`, listens on `https://localhost:5001`), in its own terminal.
- Then run the app with `dotnet run --project AeroLink.Web` in a second terminal.
- Run the tests with `dotnet test` (the HR system does not need to be running).
- The seeder skips exception #6 in the manifest because its bag belongs to flight 102, not 101, and prints a warning.
- See `README.md` for the full run steps and troubleshooting.