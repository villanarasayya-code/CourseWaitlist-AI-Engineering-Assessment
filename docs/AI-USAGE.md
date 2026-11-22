# AI Assistance and Verification Record

## AI assistance used
ChatGPT was used as an engineering assistant to help structure the bounded course-registration feature, propose the repository abstraction, implement the service and tests, and draft the design/assumptions documentation.

## Decisions reviewed by the developer

| Area | AI proposal | Decision in this implementation | How to verify |
|---|---|---|---|
| Scope | One course offering with in-memory storage | Accepted; no UI, database, payment, or catalog added | Review project structure and README |
| Persistence | Repository interface plus in-memory implementation | Accepted; service depends on `IEnrollmentRepository` | Inspect constructor and repository interface |
| Duplicate handling | Prevent registration in both enrolled and waitlisted states | Accepted; returns `AlreadyRegistered` | `Enroll_WhenLearnerAlreadyEnrolledOrWaitlisted_RejectsDuplicate` |
| FIFO | Promote earliest waitlist entry | Accepted; first list entry is considered first | `Drop_PromotesWaitlistedLearnersInFifoOrder` |
| Ineligible queue-front entry | Remove/skip and continue to next learner | Accepted; prevents queue blocking; learner may reapply after eligibility changes | `Drop_SkipsIneligibleLearnerAtFrontAndPromotesNextEligible` |
| Invalid/repeated drop | Return explicit failure without changing state | Accepted | `Drop_WhenLearnerIsNotEnrolled...` and `Drop_RepeatedDrop...` |
| Concurrency | Serialize service operations | Accepted for a single service instance; production multi-instance limitation documented | `ConcurrentEnrollments_NeverExceedCapacityOrDuplicateLearners` |

## Independent verification
Run these commands from the solution directory:

```bash
dotnet restore
dotnet build
dotnet test
```

The automated tests are the verification evidence to capture. Do not report a passing test run until these commands have been executed successfully in the target environment. Review the implementation and test assertions rather than accepting generated code without inspection.

## Human review still required
Confirm that the stated prerequisite rule and the decision to remove an ineligible learner from the waitlist match the assessment's intended policy. Confirm the target framework against the installed .NET SDK. A database-backed concurrency design is outside this bounded implementation.
