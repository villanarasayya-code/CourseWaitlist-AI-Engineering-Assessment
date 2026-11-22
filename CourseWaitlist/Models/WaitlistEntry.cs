namespace CourseWaitlist.Models;

/// <summary>One waitlist position. List insertion order is the FIFO order.</summary>
public sealed record WaitlistEntry(string LearnerId, DateTimeOffset AddedAtUtc);
