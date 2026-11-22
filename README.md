# Course Waitlist Assessment

A focused .NET 8 implementation of enrollment, dropping, and FIFO waitlist promotion for one course offering.

## Requirements
- .NET 8 SDK
- Internet access on first test run if NuGet packages are not already cached

## Run
From this folder (the folder containing `CourseWaitlist.sln`):

```bash
dotnet restore
dotnet build
dotnet test
```

## Project structure
- `CourseWaitlist/Models`: learner, enrollment, waitlist, and result models.
- `CourseWaitlist/Interfaces/IEnrollmentRepository.cs`: persistence abstraction.
- `CourseWaitlist/Repositories/InMemoryEnrollmentRepository.cs`: in-memory implementation.
- `CourseWaitlist/Services/EnrollmentService.cs`: business rules and synchronization.
- `CourseWaitlist.Tests`: focused xUnit tests.
- `docs`: design assumptions, AI usage, and limitations.

## Example usage

```csharp
using CourseWaitlist.Models;
using CourseWaitlist.Repositories;
using CourseWaitlist.Services;

var repository = new InMemoryEnrollmentRepository(capacity: 1);
var service = new EnrollmentService(repository);

Console.WriteLine(service.Enroll(new Learner("learner-1", HasMetPrerequisite: true)).Message);
Console.WriteLine(service.Enroll(new Learner("learner-2", HasMetPrerequisite: true)).Message);
Console.WriteLine(service.Drop("learner-1").Message);
```

## Important implementation decisions
- A learner may have only one active registration: enrolled or waitlisted.
- Prerequisites are checked before either enrollment or waitlisting.
- Prerequisites are checked again at promotion time using the latest saved status.
- If the first waitlisted learner is no longer eligible, that entry is removed and the next entry is considered. This prevents an ineligible learner blocking the queue.
- Dropping a learner who is not enrolled is a no-op and returns `NotEnrolled`.
- The service serializes each complete operation with a lock to prevent race conditions within one service instance/process.
