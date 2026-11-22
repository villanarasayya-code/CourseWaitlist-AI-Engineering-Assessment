using CourseWaitlist.Models;
using CourseWaitlist.Repositories;
using CourseWaitlist.Services;

namespace CourseWaitlist.Tests;

public sealed class EnrollmentServiceTests
{
    private static EnrollmentService CreateService(int capacity) =>
        new(new InMemoryEnrollmentRepository(capacity));

    [Fact]
    public void Enroll_WhenSeatIsAvailable_EnrollsLearner()
    {
        var service = CreateService(2);

        var result = service.Enroll(new Learner("L1", true));

        Assert.Equal(RegistrationOutcome.Enrolled, result.Outcome);
        Assert.Equal(new[] { "L1" }, service.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Enroll_WhenCourseIsFull_AddsLearnerToWaitlist()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));

        var result = service.Enroll(new Learner("L2", true));

        Assert.Equal(RegistrationOutcome.Waitlisted, result.Outcome);
        Assert.Equal(new[] { "L2" }, service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Enroll_WhenLearnerAlreadyEnrolledOrWaitlisted_RejectsDuplicate()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));

        var enrolledDuplicate = service.Enroll(new Learner("L1", true));
        var waitlistedDuplicate = service.Enroll(new Learner("L2", true));

        Assert.Equal(RegistrationOutcome.AlreadyRegistered, enrolledDuplicate.Outcome);
        Assert.Equal(RegistrationOutcome.AlreadyRegistered, waitlistedDuplicate.Outcome);
        Assert.Equal(new[] { "L1" }, service.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "L2" }, service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Enroll_WhenPrerequisiteIsNotMet_RejectsLearner()
    {
        var service = CreateService(1);

        var result = service.Enroll(new Learner("L1", false));

        Assert.Equal(RegistrationOutcome.PrerequisiteNotMet, result.Outcome);
        Assert.Empty(service.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Drop_PromotesWaitlistedLearnersInFifoOrder()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));
        service.Enroll(new Learner("L3", true));

        var firstDrop = service.Drop("L1");
        Assert.Equal("L2", firstDrop.PromotedLearnerId);
        Assert.Equal(new[] { "L2" }, service.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "L3" }, service.GetSnapshot().WaitlistedLearnerIds);

        var secondDrop = service.Drop("L2");
        Assert.Equal("L3", secondDrop.PromotedLearnerId);
        Assert.Equal(new[] { "L3" }, service.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Drop_SkipsIneligibleLearnerAtFrontAndPromotesNextEligible()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));
        service.Enroll(new Learner("L3", true));
        service.UpdatePrerequisiteStatus("L2", false);

        var result = service.Drop("L1");
        var snapshot = service.GetSnapshot();

        Assert.Equal("L3", result.PromotedLearnerId);
        Assert.Equal(new[] { "L3" }, snapshot.EnrolledLearnerIds);
        Assert.Empty(snapshot.WaitlistedLearnerIds);
    }

    [Fact]
    public void Drop_WhenLearnerIsNotEnrolled_ReturnsNotEnrolledWithoutChangingState()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));

        var result = service.Drop("L2");

        Assert.Equal(RegistrationOutcome.NotEnrolled, result.Outcome);
        Assert.Equal(new[] { "L1" }, service.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "L2" }, service.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Drop_RepeatedDropDoesNotPromoteTwice()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));

        var first = service.Drop("L1");
        var second = service.Drop("L1");

        Assert.Equal(RegistrationOutcome.Dropped, first.Outcome);
        Assert.Equal("L2", first.PromotedLearnerId);
        Assert.Equal(RegistrationOutcome.NotEnrolled, second.Outcome);
        Assert.Equal(new[] { "L2" }, service.GetSnapshot().EnrolledLearnerIds);
    }

    [Fact]
    public void Enroll_RejectsNullOrBlankLearner()
    {
        var service = CreateService(1);

        Assert.Equal(RegistrationOutcome.InvalidLearner, service.Enroll(null).Outcome);
        Assert.Equal(RegistrationOutcome.InvalidLearner, service.Enroll(new Learner(" ", true)).Outcome);
        Assert.Equal(RegistrationOutcome.InvalidLearnerId, service.Drop(" ").Outcome);
    }

    [Fact]
    public void UpdatePrerequisiteStatus_UpdatesKnownLearnerOnly()
    {
        var service = CreateService(1);
        service.Enroll(new Learner("L1", true));
        service.Enroll(new Learner("L2", true));

        Assert.True(service.UpdatePrerequisiteStatus("L2", false));
        Assert.False(service.UpdatePrerequisiteStatus("unknown", false));
        var dropResult = service.Drop("L1");
        var snapshot = service.GetSnapshot();

        Assert.Equal(RegistrationOutcome.Dropped, dropResult.Outcome);
        Assert.Null(dropResult.PromotedLearnerId);
        Assert.Empty(snapshot.EnrolledLearnerIds);
        Assert.Empty(snapshot.WaitlistedLearnerIds);
    }

    [Fact]
    public async Task ConcurrentEnrollments_NeverExceedCapacityOrDuplicateLearners()
    {
        const int capacity = 10;
        const int learnerCount = 100;
        var service = CreateService(capacity);

        var tasks = Enumerable.Range(1, learnerCount)
            .Select(i => Task.Run(() => service.Enroll(new Learner($"L{i}", true))))
            .ToArray();
        await Task.WhenAll(tasks);

        var snapshot = service.GetSnapshot();
        Assert.Equal(capacity, snapshot.EnrolledLearnerIds.Count);
        Assert.Equal(learnerCount - capacity, snapshot.WaitlistedLearnerIds.Count);
        Assert.Equal(learnerCount, snapshot.EnrolledLearnerIds
            .Concat(snapshot.WaitlistedLearnerIds).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Repository_RejectsZeroCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryEnrollmentRepository(0));
    }
}
