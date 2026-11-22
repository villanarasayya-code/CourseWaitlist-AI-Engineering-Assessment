namespace CourseWaitlist.Models;

/// <summary>A learner and their current prerequisite eligibility for this course.</summary>
public sealed record Learner(string Id, bool HasMetPrerequisite);
