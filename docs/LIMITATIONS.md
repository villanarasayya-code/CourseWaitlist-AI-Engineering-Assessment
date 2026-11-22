# Limitations and Risks

- Data is held only in process memory and is lost when the process stops.
- The lock protects calls made through one `EnrollmentService` instance only. Multiple service instances or application servers require transactional database coordination.
- This implementation models one course offering; it is not a multi-course registration platform.
- Prerequisite status is a simple boolean, not a full prerequisite/course-completion model.
- An ineligible learner is removed when promotion reaches them. This is an explicit policy assumption and should be confirmed with the course owner.
- There is no authentication/authorization, API, UI, audit log, retry policy, or persistent operational monitoring.
- List-based duplicate checks and removals are suitable for a small exercise, not an extremely large registration system. A production repository should use indexed queries and database constraints.
