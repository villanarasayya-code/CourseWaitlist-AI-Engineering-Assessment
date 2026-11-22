using CourseWaitlist.Models;

namespace CourseWaitlist.Interfaces;

/// <summary>Persistence boundary for the single course offering.</summary>
public interface IEnrollmentRepository
{
    int Capacity { get; }
    int EnrollmentCount { get; }
    bool IsEnrolled(string learnerId);
    bool IsWaitlisted(string learnerId);
    Learner? GetLearner(string learnerId);
    IReadOnlyList<Enrollment> GetEnrollments();
    IReadOnlyList<WaitlistEntry> GetWaitlist();
    void SaveLearner(Learner learner);
    void AddEnrollment(Enrollment enrollment);
    bool RemoveEnrollment(string learnerId);
    void AddToWaitlist(WaitlistEntry entry);
    bool RemoveFromWaitlist(string learnerId);
}
