namespace CourseWaitlist.Models;

public enum RegistrationOutcome
{
    Enrolled,
    Waitlisted,
    AlreadyRegistered,
    PrerequisiteNotMet,
    InvalidLearner,
    Dropped,
    NotEnrolled,
    InvalidLearnerId
}

public sealed record RegistrationResult(
    RegistrationOutcome Outcome,
    string Message,
    string? PromotedLearnerId = null)
{
    public bool Success => Outcome is RegistrationOutcome.Enrolled
        or RegistrationOutcome.Waitlisted
        or RegistrationOutcome.Dropped;
}
