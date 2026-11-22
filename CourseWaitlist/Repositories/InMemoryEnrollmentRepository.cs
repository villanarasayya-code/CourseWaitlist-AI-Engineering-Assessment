using CourseWaitlist.Interfaces;
using CourseWaitlist.Models;

namespace CourseWaitlist.Repositories;

/// <summary>
/// In-memory storage for one course offering. It returns copies of collections so callers
/// cannot mutate repository state without going through the repository contract.
/// </summary>
public sealed class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly Dictionary<string, Learner> _learners = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Enrollment> _enrollments = [];
    private readonly List<WaitlistEntry> _waitlist = [];

    public InMemoryEnrollmentRepository(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        Capacity = capacity;
    }

    public int Capacity { get; }
    public int EnrollmentCount => _enrollments.Count;

    public bool IsEnrolled(string learnerId) =>
        _enrollments.Any(x => string.Equals(x.LearnerId, learnerId, StringComparison.OrdinalIgnoreCase));

    public bool IsWaitlisted(string learnerId) =>
        _waitlist.Any(x => string.Equals(x.LearnerId, learnerId, StringComparison.OrdinalIgnoreCase));

    public Learner? GetLearner(string learnerId) =>
        _learners.TryGetValue(learnerId, out var learner) ? learner : null;

    public IReadOnlyList<Enrollment> GetEnrollments() => _enrollments.ToArray();
    public IReadOnlyList<WaitlistEntry> GetWaitlist() => _waitlist.ToArray();

    public void SaveLearner(Learner learner) => _learners[learner.Id] = learner;

    public void AddEnrollment(Enrollment enrollment)
    {
        if (_enrollments.Count >= Capacity)
            throw new InvalidOperationException("Course capacity has been reached.");
        if (IsEnrolled(enrollment.LearnerId) || IsWaitlisted(enrollment.LearnerId))
            throw new InvalidOperationException("Learner is already registered.");

        _enrollments.Add(enrollment);
    }

    public bool RemoveEnrollment(string learnerId)
    {
        var index = _enrollments.FindIndex(x =>
            string.Equals(x.LearnerId, learnerId, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _enrollments.RemoveAt(index);
        return true;
    }

    public void AddToWaitlist(WaitlistEntry entry)
    {
        if (IsEnrolled(entry.LearnerId) || IsWaitlisted(entry.LearnerId))
            throw new InvalidOperationException("Learner is already registered.");
        _waitlist.Add(entry);
    }

    public bool RemoveFromWaitlist(string learnerId)
    {
        var index = _waitlist.FindIndex(x =>
            string.Equals(x.LearnerId, learnerId, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _waitlist.RemoveAt(index);
        return true;
    }
}
