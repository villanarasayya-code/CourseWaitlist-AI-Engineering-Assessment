# Design Specification

## Scope
One course offering, one capacity value, enrollment, drop, prerequisite validation, and FIFO waitlist promotion. The implementation is intentionally in-memory and does not include a web API, UI, authentication, payment, course catalog, or persistent database.

## Components

### EnrollmentService
Owns business rules. It validates requests, decides whether a learner is enrolled or waitlisted, handles drops, promotes the next eligible learner, and returns explicit outcomes. It depends on `IEnrollmentRepository`, not on a concrete storage implementation.

### IEnrollmentRepository
Defines the storage operations needed by the service. This boundary allows the in-memory implementation to be replaced by a database-backed repository without moving business rules into the storage layer.

### InMemoryEnrollmentRepository
Stores learners, enrolled learners, and the ordered waitlist in memory. Waitlist list order is FIFO order. Collection reads return copies so callers cannot mutate internal lists.

## Assumptions and business rules
1. The offering is created with a positive, fixed capacity.
2. Learner ID is the unique identifier; comparison is case-insensitive.
3. A learner can be registered only once across both enrolled and waitlisted states.
4. Prerequisite eligibility is represented by `Learner.HasMetPrerequisite`.
5. A learner who does not meet the prerequisite cannot enroll or join the waitlist.
6. Eligibility is checked again when the learner reaches the front of the queue. If no longer eligible (or the learner record is missing), the waitlist entry is removed and promotion continues to the next entry. The ineligible entry does not retain its place; the learner can submit a new enrollment request after becoming eligible.
7. A successful drop removes the enrolled learner and fills the seat with the earliest eligible waitlisted learner, if one exists.
8. A drop for a learner who is not enrolled returns `NotEnrolled` and changes no state. This includes a repeated drop.
9. Invalid IDs and null learner requests return explicit outcomes and do not change registration state.
10. Waitlist ordering is based on the order in which valid full-capacity enrollment requests are processed.

## Concurrency
A private `lock` in `EnrollmentService` protects each complete read/validate/write operation. This prevents two concurrent calls through the same service instance from exceeding capacity or registering the same learner twice. The guarantee is process-local and assumes one service instance coordinates access to this repository. A production deployment with multiple application instances requires database transactions/locking or another distributed coordination strategy; an in-process lock is not sufficient across servers.

## Complexity
Let `E` be the number of enrolled learners and `W` the number of waitlisted learners.
- Enroll: O(E + W) in this simple list/dictionary-backed implementation because duplicate checks scan the lists; learner lookup/update is average O(1).
- Drop: O(E + W) for locating/removing the learner and scanning waitlist entries; each skipped ineligible entry is removed once.
- Snapshot: O(E + W) to copy the two lists.
- Space: O(L), where L is the number of learners known to this offering.

For a high-volume production system, use indexed tables/unique constraints and an ordered waitlist query with an index on offering ID and queue sequence.
