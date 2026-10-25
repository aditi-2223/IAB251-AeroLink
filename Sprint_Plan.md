# AeroLink Sprint Plan

IAB251 Assessment 2. Team of 2: **Aditi Vansjaliya (Member A)** and **Hetvik Desai (Member B)**.

**Status key:** ✅ done and merged, 🔄 in progress, 🔲 not started

**Scope:** The team is two people, so the scope is T1, T2, T3, I1, I2, I3 and I4. The workflow ends after reporting an exception (I4).

---

## 1. MVP definition

Make the whole workflow clickable from start to finish first, then polish each part. 

**The MVP path (what we must be able to demo):**

```
Sign in → Main screen → Select flight → View manifest → Verify and load a bag
        → Acknowledge special handling → Report an exception → Log out
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
| 7 | The user can log out and protected pages are no longer reachable | T3 | Aditi |
| 8 | Each person has a unit test suite for their stories | I1, I2, I3, I4 | Both |

---

## 2. Sprint 1: foundation and first screens

**Sprint dates:** 05-10-26 to 12-10-26

**Sprint goal:** a signed-in user can reach the main screen and view the manifest for a flight, and the loading page exists so the special-handling and exception work can plug into it.

### Aditi (Member A)

| Story | Tasks | Status |
|---|---|---|
| **I1** View expected bags | Data models and enums, `AeroLinkDbContext`, `DbSeeder` (loads the manifest xlsx), `FlightService` + `IFlightService`, `Flights` page, `Manifest` page | ✅ |
| **T1** Main screen and navigation | Cookie authentication in `Program.cs`, `/Baggage` locked behind sign-in, navbar with sign-in state, main screen with module tiles, temporary sign-in page | ✅ |

Supporting work finished this sprint: `README.md`, `PROJECT_STRUCTURE.md`, `.gitignore` fix, `global.json` pinning .NET 8.

### Hetvik (Member B)

| Story | Tasks | Status |
|---|---|---|
| **I2** Verify and confirm a bag | `IBagVerificationService` + `BagVerificationService`, `Loading` page (tag entry and confirm), rejection rules (invalid, wrong flight, duplicate), `BagVerificationServiceTests` | ✅ |

## | What went well | What was hard | What we changed 
 -> One member completed both of her stories (I1 and T1) on time, along with the supporting setup (README, project structure, .NET version pinning). This gave the team a solid foundation and a clear base for the remaining stories. 

-> Progress was uneven across the team, so the sprint plan needed adjusting to stay in line with the real status. Because I3 and I4 depend on I2, we had to manage those dependencies carefully. 

 -> We rebalanced the workload to share the stories more evenly. T2 was carried over into Sprint 2.

## 3. Sprint 2: special handling and exceptions

**Sprint dates:** 14-10-26 to 23-10-26

**Sprint goal:** the full MVP path works end to end, and each person has their unit tests.

### Aditi (Member A)

| Story | Tasks | Status |
|---|---|---|
| **T3** Log out | `Logout` page (POST), navbar Log out button | ✅ |
| **I3** Special handling | `ISpecialHandlingService` + `SpecialHandlingService`, category and instruction on the `Manifest` page, instruction panel and acknowledgement checkbox on the `Loading` page (Confirm stays disabled until ticked), `SpecialHandlingServiceTests`, plus `FlightServiceTests` for I1 | ✅ |

### Hetvik (Member B)

| Story | Tasks | Status |
|---|---|---|
| **T2** HR login | Replace the temporary `Account/Login`, call the HR API in `HrAuthService`, show errors for empty or invalid input, `HrAuthServiceTests` | ✅ |
| **I4** Report a problem | `ExceptionService`, report form on the `Loading` page, `ExceptionServiceTests` | ✅ |
| Any unfinished Sprint 1 stories  |

## | What went well | What was hard | What we changed 
-> All in-scope stories were merged and the full workflow runs from sign-in to log out. Building I2 before I3 and I4 kept the dependencies in order, and each member now has their own unit test suite. Twice-weekly check-ins and pull request reviews kept `main` building. 

-> Connecting the real HR login (T2) added a dependency on the separate HR API, so it needs to be running before AeroLink starts. Three stories (I2, I3 and I4) shared the `Loading` page, which meant coordinating changes closely. We also kept the documentation updated alongside the code. 

-> We documented the run order in the README (start the HR system first, then AeroLink), so the app is easy to start and demo. Coordinating on the `Loading` page worked well, and we would use the same approach on future shared pages. 

### Order of work and dependencies

```
T1 ✅ → T2 (B replaces the temporary sign-in)
I1 ✅ → I2 (B) → I3 part 2 (A, adds the panel to the Loading page)
              → I4 (B, report form on the Loading page)
I3 part 1 (A) is independent: service, tests and manifest display
T3 ✅ is independent
```

---

## 4. Team working agreement

- Each story gets its own branch (for example `feature/I3-special-handling`), cut from a clean, up-to-date `main`.
- Commit messages name the story, for example `Add SpecialHandlingService (I3)`.
- Every change goes through a pull request. Ask the other person to review before merging whenever possible.
- Move the board task to **Active** when you start and **Closed** when merged.
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



