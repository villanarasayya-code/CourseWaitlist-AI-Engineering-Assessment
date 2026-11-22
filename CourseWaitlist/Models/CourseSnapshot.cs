namespace CourseWaitlist.Models;

/// <summary>Read-only view of course registration state.</summary>
public sealed record CourseSnapshot(
    int Capacity,
    IReadOnlyList<string> EnrolledLearnerIds,
    IReadOnlyList<string> WaitlistedLearnerIds);
