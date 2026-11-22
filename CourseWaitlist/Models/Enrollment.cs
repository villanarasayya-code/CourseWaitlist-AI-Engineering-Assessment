namespace CourseWaitlist.Models;

public sealed record Enrollment(string LearnerId, DateTimeOffset EnrolledAtUtc);
