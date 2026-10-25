# AeroLink Acceptance Criteria

IAB251 Assessment 2. Team: Aditi Vansjaliya (Member A) and Hetvik Desai (Member B).

This document lists what each user story must do before we call it finished. The stories in scope are T1, T2, T3, I1, I2, I3 and I4. Stories I5, I6 and T4 are out of scope, so the workflow ends after a handler reports a problem with a bag.

A story is done when every point under it is true, and it also meets the definition of done in `sprint_plan.md`.

| Story | What it is | Owner |
|---|---|---|
| T1 | Main screen and navigation | Aditi |
| T2 | Employee login using the HR system | Hetvik |
| T3 | Log out | Aditi |
| I1 | View expected bags | Aditi |
| I2 | Verify and confirm a bag | Hetvik |
| I3 | Special handling | Aditi |
| I4 | Report a problem | Hetvik |

---

## T1: Main screen and navigation

- **AC1.** The main screen (`/`) is where everyone starts. It shows a tile for each of the three modules: Turnaround Planning and Resource Allocation, Baggage Unloading, Transfer and Loading Management, and Departure Readiness and Turnaround Completion.
- **AC2.** The Baggage tile takes the user to `/Baggage/Flights`. The other two tiles are shown but do nothing in this prototype, and they never lead to a broken page.
- **AC3.** Someone who is not signed in sees a sign-in option on the main screen and in the navbar. They can't see any flight, bag or exception information until they sign in.
- **AC4.** Once signed in, the navbar shows who is signed in and a Log out button instead of the sign-in option.
- **AC5.** Every page under `/Baggage` needs sign-in. If a signed-out person types one of these addresses directly, they are sent to `/Account/Login`.
- **AC6.** Cookie authentication is set up once in `Program.cs` using the default scheme. The login page only has to call `HttpContext.SignInAsync` with that scheme.
- **AC7.** The navbar and layout look the same on every page, and nothing links to the old `Privacy` page.
- **AC8.** The pages stay simple. The main screen and layout contain no business rules.

---

## T2: Employee login using the HR system

- **AC1. Handler signs in.** If I am a Baggage Handler in the HR system and I enter my correct email and Employee ID, I am signed in, taken to the main screen, and I can open the Baggage module.
- **AC2. Supervisor signs in.** If my HR role is BG_Supervisor and I enter my correct email and Employee ID, I am signed in and get the same access to the Baggage module as a handler.
- **AC3. Employee not found.** If my email and Employee ID don't match anyone in the HR system, I stay on the login page, I am not signed in, and I see an error like "Invalid email or employee ID."
- **AC4. Email and ID don't match.** If the email belongs to one person and the ID belongs to someone else, the login is rejected with the same general error. The message must not say which field was wrong.
- **AC5. Wrong role.** If I exist in the HR system but my role is not Baggage Handler or BG_Supervisor, I am not signed in and I see a message like "You do not have permission to access the Baggage module."
- **AC6. Empty fields.** If I leave the email, the Employee ID, or both empty and click Sign in, nothing is sent to the HR API. A message appears next to each empty field, such as "Email is required" or "Employee ID is required."
- **AC7. Wrong format.** If the email isn't a valid email (for example "abc" or "abc@"), or the Employee ID isn't a whole number (for example "ABC12" or "10.5"), the HR API is not called and I am told which field is wrong. Spaces before or after what I type are removed before checking.
- **AC8. HR system unavailable.** If the HR API is off, times out or returns an unexpected error, I am not signed in, the app doesn't crash, and I see a message like "Unable to verify your details right now. Please try again later."
- **AC9. Session and access.** After a successful sign-in, my name, Employee ID and role are stored in the login cookie, and the navbar shows I am signed in with a Log out button. If I'm not signed in and open `/Baggage/Flights` directly, I am sent to the login page.
- **AC10. Replaces the temporary login.** The old "accept any name" login is gone. The new login uses the existing cookie scheme, so `Program.cs` doesn't need to change.
- **AC11. Code and tests.**
  - `IHrAuthService` and `HrAuthService` have full XML doc comments.
  - `HrAuthServiceTests` cover at least: valid handler, valid supervisor, unknown employee, wrong role, empty inputs, invalid format, and HR API failure. The tests use a mocked HTTP client, so they don't need the real HR API.
  - The code builds, tests pass, the work is merged through a pull request, and the board task is closed.

---

## T3: Log out

- **AC1.** A signed-in user sees a Log out button in the navbar on every page. Someone who is signed out does not.
- **AC2.** Clicking Log out sends a POST request to `/Account/Logout`. Typing `/Account/Logout` into the address bar does not sign anyone out.
- **AC3.** Logging out removes the login cookie and ends the session.
- **AC4.** After logging out, the user is taken to `/Account/Login`.
- **AC5.** After logging out, the navbar shows the signed-out view (sign-in option, no Log out button).
- **AC6.** After logging out, protected pages can't be reached. Opening or refreshing `/Baggage/Flights`, `/Baggage/Manifest` or `/Baggage/Loading` sends the user to the login page. This is also true after pressing the browser's Back button and refreshing.
- **AC7.** Logging out doesn't change any data. Bag outcomes, loaded totals and exceptions are all still there when the user signs back in.
- **AC8.** The user can sign in again straight after logging out.

---

## I1: View expected bags

### Choosing a flight

- **AC1.** A signed-in handler can open `/Baggage/Flights` and see the flights from the manifest. Each flight shows its flight number, scheduled departure time and loading status.
- **AC2.** Each flight links to its manifest at `/Baggage/Manifest?flightId=…`. The chosen flight is passed in the address.
- **AC3.** If there are no flights, the page shows a clear message instead of an empty or broken table.

### Viewing the manifest

- **AC4.** The Manifest page shows the flight number and the list of bags expected on that flight.
- **AC5.** Each bag shows its tag, its baggage (handling) type and its current loading status, such as Pending or Loaded.
- **AC6.** The page shows "Loaded x of y". The number matches the statuses in the list and the total on the Loading page.
- **AC7.** It's easy to tell loaded bags from bags still to load.
- **AC8.** Only bags for the chosen flight are shown. Bags from other flights never appear.
- **AC9.** The Manifest page has a link to the Loading page for the same flight, so the handler can go straight on to checking bags.
- **AC10.** An unknown or missing `flightId` gives a 404 page.
- **AC11.** Both pages need sign-in. A signed-out visitor is sent to the login page.

### Data and code

- **AC12.** On the first run, `DbSeeder` creates `AeroLink.db` and loads the flights, bags and exceptions from the supplied manifest spreadsheet. Running it again doesn't create duplicates.
- **AC13.** The seeder skips exception #6 because its bag belongs to flight 102, not 101, and prints a warning. The app still starts normally.
- **AC14.** `IFlightService` and `FlightService` provide the list of flights and the manifest for a flight, and are registered in `Program.cs`. The pages only call the service and contain no business rules.
- **AC15.** `FlightServiceTests` cover listing flights, getting the manifest for a flight, and an unknown flight.
- **AC16.** Every public member in `Models/`, `Data/` and `Services/` has an XML doc comment.

---

## I2: Verify and confirm a bag

### Entering and checking a tag

- **AC1.** A signed-in handler can open the Loading page for a chosen flight. It shows the flight number, a box to enter a bag tag, and the current "loaded X of Y" total.
- **AC2.** If the tag exists on this flight and its outcome is Pending, verifying it shows the tag, the handling type and a Confirm loaded button.
- **AC3.** If the tag is empty or only spaces, the page says "Please enter a bag tag" and does nothing else.
- **AC4.** Tag matching ignores spaces at the start or end, and ignores upper or lower case.

### Confirming

- **AC5.** When I confirm a verified bag, its outcome becomes Loaded in the database, the loaded total goes up by exactly 1, and a success message names the tag.
- **AC6.** The new total is still correct after refreshing the page, and also on the Manifest page.
- **AC7.** The first confirmed bag changes the flight's loading status from NotStarted to InProgress.

### Rejections (the loaded count must never change)

- **AC8. Invalid tag.** A tag that isn't in the dataset is rejected with "Tag not found".
- **AC9. Wrong flight.** A tag that exists but belongs to another flight is rejected with a message that names that flight, for example DEMO102.
- **AC10. Duplicate.** A tag that is already Loaded is rejected with "Already loaded".
- **AC11. Open exception.** A bag with an open exception is rejected with "Unresolved exception, see supervisor".
- **AC12.** A rejected attempt changes no bag outcomes and not the total. The page stays usable and ready for the next tag.

### Safety and access

- **AC13.** The service checks everything on the server. If someone posts a confirm request for a bag that is already loaded, belongs to another flight, or doesn't exist, it is rejected too.
- **AC14.** If the same tag is submitted twice quickly, such as a double-click, the count goes up only once.
- **AC15.** The Loading page needs sign-in. An unknown `flightId` gives a 404 page.
- **AC16.** For a special bag (any handling type other than Standard), Confirm is not allowed unless it has been acknowledged. The acknowledgement panel itself is built in I3.

---

## I3: Special handling

### Spotting special bags on the manifest

- **AC1.** A bag is "special" when its handling type is anything other than Standard. This rule lives in `SpecialHandlingService`, not in the pages.
- **AC2.** On the Manifest page, each special bag shows its special-handling category and its handling instruction.
- **AC3.** Standard bags show no category or instruction, so special bags stand out.
- **AC4.** A special bag with a missing or blank instruction is still treated as special. The page shows a short fallback message and doesn't crash.

### Acknowledging on the Loading page

- **AC5.** When a handler verifies a special bag, an instruction panel appears with the bag's category, its handling instruction and an acknowledgement checkbox.
- **AC6.** The Confirm loaded button stays disabled until the checkbox is ticked.
- **AC7.** Once the checkbox is ticked, Confirm loaded works. Confirming loads the bag, adds exactly 1 to the loaded total and shows the usual success message.
- **AC8.** A standard bag shows no panel and no checkbox, and can be confirmed without acknowledging anything, as before.
- **AC9.** The acknowledgement doesn't carry over. When the next special bag is verified, the panel appears again with the checkbox empty.

### Enforced on the server

- **AC10.** The service enforces the rule, not just the page. If a confirm request for a special bag is sent without acknowledgement (for example posted directly), it is rejected with a clear message. The bag stays Pending and the total doesn't change.
- **AC11.** The acknowledgement rule works alongside the I2 rejections. An invalid, wrong-flight, duplicate or open-exception special bag is still rejected, whether or not it was acknowledged.
- **AC12.** Acknowledging doesn't change the bag's data. It only lets the confirmation go ahead.

### Code and tests

- **AC13.** `ISpecialHandlingService` and `SpecialHandlingService` are registered in `Program.cs` and have full XML doc comments.
- **AC14.** `SpecialHandlingServiceTests` cover at least: a standard bag, a special bag, a special bag with a missing instruction, and confirming with and without acknowledgement.
- **AC15.** The branch is cut from an up-to-date `main`, commits name the story (for example `Add SpecialHandlingService (I3)`), and the work is merged through a pull request.

---

## I4: Report a problem

### The report form

- **AC1.** `/Baggage/Loading?flightId=…` shows a "Report a problem" section where the placeholder used to be. It displays the flight number, and the flight comes from the page's `flightId`.
- **AC2.** The form has a tag box, a category dropdown (MissingBag, DamagedBag) and a comment box. It uses its own properties and handler (`ReportTag`, `ReportCategory`, `ReportComment`, `OnPostReportAsync`), so it doesn't clash with the existing Tag and Acknowledged fields or the Verify and Confirm handlers.
- **AC3.** A blank, null or spaces-only tag is rejected with a message, and nothing is saved.
- **AC4.** An unknown tag is rejected with "Tag not found". A tag from another flight is rejected with the same wording as I2 ("Tag belongs to flight DEMO102"). Tag matching ignores spaces and case, like I2.
- **AC5.** A blank or spaces-only comment is rejected. A comment longer than 500 characters is rejected. A comment of exactly 500 characters is accepted.
- **AC6.** An invalid category is rejected, even if it was posted directly and not chosen from the dropdown.
- **AC7.** The service checks everything itself. The page and its HTML limits are only there to help the user.
- **AC8.** On success, the page shows a message naming the bag tag and clears the form. On failure, it shows the error and keeps what the user typed.
- **AC9.** Reporting a problem doesn't reset the "Loaded x of y" count or any verification result already on the page.

### What gets saved

- **AC10.** A successful report adds one row to Exceptions with the right BagId and FlightId, the chosen Category, the trimmed comment as Description, Status = Open, DecisionReason = null, and ReportedAtUtc filled in.
- **AC11.** The service sets ReportedAtUtc from the UTC clock. The user can't supply it. The time can be tested, for example by passing an optional `TimeProvider` into the constructor.
- **AC12.** The bag tag is stored through BagId, so it can be shown through the Bag navigation property.
- **AC13.** Reporting a problem doesn't change the bag's Outcome. It stays Pending, and ApprovedNotToLoad is never used (it belongs to a removed story).
- **AC14.** A bag that already has an open exception can't be reported again. The message says it has already been reported. This avoids duplicates and matches I2's rule of one open exception per bag.
- **AC15.** **Decision to log on the board:** can a bag that is already Loaded be reported? We recommend rejecting it with an "already loaded" message, because a loaded bag isn't missing.

### Visible to the Supervisor

The prototype has no supervisor screen. "Visible to the Supervisor" means the exception is saved as Open, can be looked up per flight, and is shown on the Loading page.

- **AC16.** `IExceptionService.GetOpenExceptionsAsync(flightId)` returns the open exceptions for that flight, with bag tag, category, comment and reported time, newest first. It leaves out other flights' exceptions and any that aren't open.
- **AC17.** The Loading page lists these open exceptions for the chosen flight, including the seeded ones. If ReportedAtUtc is null, it shows "Not recorded" and doesn't crash.
- **AC18.** After a report is made, the new exception appears in the list straight away.
- **AC19.** If a bag is reported and then verified or confirmed in the same session, the page shows I2's "Unresolved exception, see supervisor" message. This checks that the existing behaviour still works. It is not new code.

### Blocking completion

- **AC20.** `IExceptionService.HasOpenExceptionsAsync(flightId)` returns true if the flight has any open exception, and false if it has none.
- **AC21.** It only looks at the chosen flight. An open exception on flight 102 doesn't affect flight 101.
- **AC22.** While a flight has open exceptions, the Loading page shows a warning banner saying loading can't be completed until they are resolved. This is the part the user can see.
- **AC23.** **Note for the report:** there is no completion action in this scope (I5 is excluded), so there is nothing to disable. The rule is built into the service and tested, ready for I5 to use.

### Code and process

- **AC24.** New files: `Services/IExceptionService.cs`, `ExceptionService.cs`, a result type (for example `ExceptionReportResult` with an outcome enum, like `VerificationResult`), and an `ExceptionCategories` constants class (like `HandlingTypes`).
- **AC25.** The service is registered in `Program.cs` with `AddScoped<IExceptionService, ExceptionService>()`.
- **AC26.** `LoadingModel`'s constructor takes `IExceptionService`. All business rules stay in the service.
- **AC27.** Every public member has full XML doc comments (purpose, parameters, returns, pre-conditions and post-conditions).
- **AC28.** The branch is `feature/I4-report-exception`, cut from an up-to-date `main`. Commits name the story (for example `Add ExceptionService (I4)`). The work is merged through a pull request, and the other team member reviews it because both of us edit `Loading.cshtml`.
- **AC29.** Update `PROJECT_STRUCTURE.md` and `README.md` (I4 status), move the board task to Closed, and log the AC12 and AC15 decisions in Discussion.