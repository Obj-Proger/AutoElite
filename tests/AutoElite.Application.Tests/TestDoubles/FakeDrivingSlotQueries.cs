using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingSlots;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeDrivingSlotQueries : IDrivingSlotQueries
{
    private readonly Dictionary<Guid, DrivingSlotDetails> _details = [];

    public List<StudentDrivingLessonItem> StudentLessons { get; } = [];

    public (DrivingScheduleFilter Filter, DataScope Scope)? LastScheduleCall { get; private set; }

    public (AvailableDrivingSlotFilter Filter, DataScope Scope)? LastAvailableCall { get; private set; }

    public (Guid StudentId, TimeInterval Period)? LastStudentCall { get; private set; }

    public void Seed(DrivingSlotDetails details) => _details.Add(details.Slot.Id, details);

    public Task<IReadOnlyList<DrivingScheduleItem>> GetScheduleAsync(
        DrivingScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastScheduleCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<DrivingScheduleItem>>([]);
    }

    public Task<IReadOnlyList<DrivingSlotItem>> GetAvailableAsync(
        AvailableDrivingSlotFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastAvailableCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<DrivingSlotItem>>([]);
    }

    public Task<IReadOnlyList<StudentDrivingLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default)
    {
        LastStudentCall = (studentId, period);

        return Task.FromResult<IReadOnlyList<StudentDrivingLessonItem>>([.. StudentLessons]);
    }

    public Task<DrivingSlotDetails?> GetDetailsAsync(Guid slotId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(slotId));
}
