using CourseWaitlist.Interfaces;
using CourseWaitlist.Models;

namespace CourseWaitlist.Services;

/// <summary>Coordinates registration rules for one course offering.</summary>
public sealed class EnrollmentService
{
    private readonly IEnrollmentRepository _repository;
    // Serializes the complete read/validate/write workflow within this service instance.
    private readonly object _sync = new();

    public EnrollmentService(IEnrollmentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public RegistrationResult Enroll(Learner? learner)
    {
        if (learner is null || string.IsNullOrWhiteSpace(learner.Id))
            return new(RegistrationOutcome.InvalidLearner, "A valid learner ID is required.");

        lock (_sync)
        {
            if (_repository.IsEnrolled(learner.Id) || _repository.IsWaitlisted(learner.Id))
                return new(RegistrationOutcome.AlreadyRegistered,
                    $"Learner '{learner.Id}' is already enrolled or waitlisted.");

            // Store the latest eligibility so it can be rechecked if this learner reaches the queue front.
            _repository.SaveLearner(learner);

            if (!learner.HasMetPrerequisite)
                return new(RegistrationOutcome.PrerequisiteNotMet,
                    $"Learner '{learner.Id}' has not met the course prerequisite.");

            if (_repository.EnrollmentCount < _repository.Capacity)
            {
                _repository.AddEnrollment(new Enrollment(learner.Id, DateTimeOffset.UtcNow));
                return new(RegistrationOutcome.Enrolled, $"Learner '{learner.Id}' enrolled successfully.");
            }

            _repository.AddToWaitlist(new WaitlistEntry(learner.Id, DateTimeOffset.UtcNow));
            return new(RegistrationOutcome.Waitlisted,
                $"Course is full. Learner '{learner.Id}' was added to the waitlist.");
        }
    }

    public RegistrationResult Drop(string? learnerId)
    {
        if (string.IsNullOrWhiteSpace(learnerId))
            return new(RegistrationOutcome.InvalidLearnerId, "A valid learner ID is required.");

        lock (_sync)
        {
            if (!_repository.RemoveEnrollment(learnerId))
                return new(RegistrationOutcome.NotEnrolled,
                    $"Learner '{learnerId}' is not currently enrolled.");

            var promotedLearnerId = PromoteNextEligibleLearner();
            var message = promotedLearnerId is null
                ? $"Learner '{learnerId}' was dropped. No eligible learner was available to promote."
                : $"Learner '{learnerId}' was dropped. Learner '{promotedLearnerId}' was promoted from the waitlist.";

            return new(RegistrationOutcome.Dropped, message, promotedLearnerId);
        }
    }

    /// <summary>
    /// Updates prerequisite status for a learner already known to the course workflow.
    /// Promotion always checks the latest saved status, not only the status at queue entry.
    /// </summary>
    public bool UpdatePrerequisiteStatus(string? learnerId, bool hasMetPrerequisite)
    {
        if (string.IsNullOrWhiteSpace(learnerId)) return false;

        lock (_sync)
        {
            var learner = _repository.GetLearner(learnerId);
            if (learner is null) return false;
            _repository.SaveLearner(learner with { HasMetPrerequisite = hasMetPrerequisite });
            return true;
        }
    }

    public CourseSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new CourseSnapshot(
                _repository.Capacity,
                _repository.GetEnrollments().Select(x => x.LearnerId).ToArray(),
                _repository.GetWaitlist().Select(x => x.LearnerId).ToArray());
        }
    }

    private string? PromoteNextEligibleLearner()
    {
        while (_repository.EnrollmentCount < _repository.Capacity && _repository.GetWaitlist().Count > 0)
        {
            // The first item is always the oldest item: strict FIFO.
            var next = _repository.GetWaitlist()[0];
            _repository.RemoveFromWaitlist(next.LearnerId);

            var learner = _repository.GetLearner(next.LearnerId);
            if (learner is null || !learner.HasMetPrerequisite)
            {
                // Remove an ineligible queue-front entry so it cannot block eligible learners behind it.
                continue;
            }

            _repository.AddEnrollment(new Enrollment(learner.Id, DateTimeOffset.UtcNow));
            return learner.Id;
        }

        return null;
    }
}
