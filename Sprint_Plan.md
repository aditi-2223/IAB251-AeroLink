# AeroLink Sprint Plan

IAB251 Assessment 2. Team of 2: **Aditi Vansjaliya (Member A)** and **Hetvik Desai (Member B)**.

**Status key:** ✅ done and merged, 🔄 in progress, 🔲 not started

**Scope:** Stories I5 (supervisor decision) and I6 (close loading) are removed from Assessment 2 (confirmed with the tutor). The workflow ends after reporting an exception (I4) and viewing the status report (T4).

---

## 1. MVP definition

Make the whole workflow clickable from start to finish first, then polish each part. A rough screen that connects to the next one is worth more than a polished screen that nothing links to.

**The MVP path (what we must be able to demo):**

```
Sign in → Main screen → Select flight → View manifest → Verify and load a bag
        → Acknowledge special handling → Report an exception → View status report → Log out
```

**MVP is done when:**

| # | Check | Story | Owner |
|---|---|---|---|
| 1 | An employee can sign in (HR check, with an error message on failure) | T2 | Hetvik |
| 2 | The main screen shows the module tiles and links to Baggage | T1 | Aditi |
| 3 | A handler can pick a flight and see its expected bags with type and status | I1 | Aditi |
| 4 | A valid tag can be confirmed as loaded; invalid, wrong-flight and duplicate tags are rejected | I2 | Hetvik |
| 5 | A special bag shows its instruction and cannot be loaded until acknowledged | I3 | Aditi |
| 6 | A handler can report a problem with a bag and it is stored as an open exception | I4 | Hetvik |
| 7 | The status report shows expected, loaded and outstanding counts and open exceptions | T4 | Hetvik |
| 8 | The user can log out and protected pages are no longer reachable | T3 | Aditi |
| 9 | Each person has a unit test suite for their stories | I1, I3 / I2, I4 | Both |

---

## 2. Sprint 1 : foundation and first screens

**Sprint dates:** 05-10-26 to 11-10-26

**Sprint goal:** a signed-in user can reach the main screen and view the manifest for a flight, and the loading page exists so the special-handling and exception work can plug into it.

### Aditi (Member A)

| Story | Tasks | Status |
|---|---|---|
| **I1** View expected bags | Data models and enums, `AeroLinkDbContext`, `DbSeeder` (loads the manifest xlsx), `FlightService` + `IFlightService`, `Flights` page, `Manifest` page | ✅ |
| **T1** Main screen and navigation | Cookie authentication in `Program.cs`, `/Baggage` locked behind sign-in, navbar with sign-in state, main screen with module tiles, temporary sign-in page | ✅ |

Supporting work finished this sprint: `README.md`, `PROJECT_STRUCTURE.md`, `.gitignore` fix, `global.json` pinning .NET 8.

### Hetvik (Member B)

Order matters: **do I2 first**, because I3, I4 and T4 all build on the `Loading` page and `BagVerificationService`. The temporary sign-in keeps the app usable until T2 lands.

| Story | Tasks | Status |
|---|---|---|
| **I2** Verify and confirm a bag | `IBagVerificationService` + `BagVerificationService`, `Loading` page (tag entry and confirm), rejection rules (invalid, wrong flight, duplicate), `BagVerificationServiceTests` | 🔲 |
| **T2** HR login | Replace the temporary `Account/Login`, call the HR API in `HrAuthService`, show errors for empty or invalid input, `HrAuthServiceTests` | 🔲 |

### Decisions and notes (also logged on the board)

- Seed data: exception #6 in the manifest says flight 101 but its bag belongs to flight 102, so the seeder skips it and logs a warning.
- `BagException` needed an explicit `HasKey(e => e.ExceptionId)` because its key name does not follow EF naming rules.
- Cookie authentication uses the default scheme. T2 only has to call `HttpContext.SignInAsync` with that scheme, so `Program.cs` does not need to change.
- Special-handling rules live in their own class (`SpecialHandlingService`) so I2 and I3 do not edit the same file.

---

## 3. Sprint 2 (next): exceptions, status and special-handling screen

**Sprint dates:** ____ to ____

**Sprint goal:** the full MVP path works end to end, and each person has their unit tests.

### Aditi (Member A)

| Story | Tasks | Status |
|---|---|---|
| **T3** Log out | `Logout` page (POST), navbar Log out button | 🔄 |
| **I3** Special handling (part 1) | `ISpecialHandlingService` + `SpecialHandlingService`, show category and instruction on the `Manifest` page, `SpecialHandlingServiceTests` | 🔲 |
| **I3** Special handling (part 2) | After I2 is merged: show the instruction panel and an acknowledgement checkbox on the `Loading` page, keep Confirm disabled until it is ticked | 🔲 |
| **I1 tests** | `FlightServiceTests`: list flights, manifest for a flight, unknown flight | 🔲 |

### Hetvik (Member B)

| Story | Tasks | Status |
|---|---|---|
| **I4** Report a problem | `ExceptionService`, report form on the `Loading` page, `ExceptionServiceTests` | 🔲 |

### Order of work and dependencies

```
T1 ✅ → T2 (B replaces the temporary sign-in)
I1 ✅ → I2 (B) → I3 part 2 (A, adds the panel to the Loading page)
              → I4 (B, report form on the Loading page)
I3 part 1 (A) is independent: service, tests and manifest display
T3 🔄 is independent
```

## 4. Team working agreement

- Each story gets its own branch (for example `feature/I3-special-handling`), cut from a clean, up-to-date `main`.
- Commit messages name the story, for example `Add SpecialHandlingService (I3)`.
- Every change goes through a pull request. Ask the other person to review before merging whenever possible.
- Move the board task to **Active** when you start and **Closed** when merged. Add a Discussion comment for blockers and decisions.
- Public members in `Models/`, `Data/` and `Services/` need full XML doc comments.
- `main` must always build and run.
- Short check-in twice a week: what is done, what is next, what is blocking.

---

## 5. Definition of done (per story)

- Code builds with no errors and the page works in the browser.
- Unit tests pass where the story has a service to test.
- XML doc comments are written.
- `PROJECT_STRUCTURE.md` is updated.
- Pull request merged, board task closed, and any decisions logged in Discussion.

---

## 6. Reflection log (for the Agile marks)

Add a line at the end of each sprint.

| Sprint | What went well | What was hard | What we change |
|---|---|---|---|
| 1 | | | |
| 2 | | | |