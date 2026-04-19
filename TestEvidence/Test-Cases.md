# Blind-Match PAS Test Case Table

This table maps directly to the testing strategy categories in the project plan.

| ID | Objective | Steps | Expected | Actual |
|---|---|---|---|---|
| UT-01 | Verify matching logic confirms match only after interest | Seed proposal (UnderReview) and supervisor interest, call `ConfirmMatch` | Proposal status becomes `Matched`, `MatchAssignment` is created, audit record exists | Pass |
| UT-02 | Verify proposal transition from Pending to UnderReview | Seed `Pending` proposal, call `ExpressInterest` | Proposal status changes to `UnderReview`, supervisor interest is stored | Pass |
| UT-03 | Verify reveal trigger logic after confirmed match | Seed proposal + match, call `StudentController.Details` | Student and supervisor identities are visible in returned `MatchDetailsViewModel` | Pass |
| UT-04 | Verify reveal remains hidden before match | Seed proposal without match, call `StudentController.Details` | Identity fields are null in `MatchDetailsViewModel` | Pass |
| VT-01 | Validate required field handling on student proposal create | Add model error, call `StudentController.Create` | Returns same view and does not persist proposal | Pass |
| VT-02 | Validate invalid transition (edit after matched) is blocked | Seed `Matched` proposal, call `StudentController.Edit` | Redirects without changing persisted proposal | Pass |
| IT-01 | Verify role-based route protection for Supervisor module | Access `/Supervisor/Index` with Student role | Request is forbidden (403) | Pass |
| IT-02 | Verify role-based route protection for Student module | Access `/Student/Index` with Supervisor role | Request is forbidden (403) | Pass |
| ST-01 | Verify unauthorized access attempt behavior | Access `/Dashboard/Index` without authentication | Request is challenged/unauthorized (401) | Pass |
| ST-02 | Verify authenticated session routing behavior | Access `/Dashboard/Index` with Student role | Redirects to Student workspace route | Pass |

## Traceability
- Total automated tests implemented: 10
- Unit + validation + integration + security checks are all covered by executable xUnit tests.
