using AutoElite.Application.Contracts;
using AutoElite.Application.Settings;

namespace AutoElite.Application.DrivingSlots.CompleteDrivingLesson;

/// <summary>Creates the grades and completes the lesson on the slot and on the contract in one unit of work.</summary>
internal sealed class CompleteDrivingLessonCommandHandler(
    DrivingSlotAccess slotAccess,
    IContractRepository contractRepository,
    SchoolSettingsProvider settingsProvider,
    TimeProvider timeProvider) : ICommandHandler<CompleteDrivingLessonCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CompleteDrivingLessonCommand command, CancellationToken cancellationToken)
    {
        var slotResult = await slotAccess.LoadOwnAsync(command.SlotId, cancellationToken);
        if (slotResult.IsFailure)
            return slotResult;

        var gradesResult = CreateGrades(command.Grades ?? []);
        if (gradesResult.IsFailure)
            return gradesResult;

        var slot = slotResult.Value;
        var contractResult = await BookedContract.LoadAsync(slot, contractRepository, cancellationToken);
        if (contractResult.IsFailure)
            return contractResult;

        var settings = await settingsProvider.GetAsync(cancellationToken);

        return slot.Complete(
            contractResult.Value, command.ActualHours, gradesResult.Value, command.Note, settings.QualityWindowSize,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private static Result<IReadOnlyList<SkillGrade>> CreateGrades(IReadOnlyList<SkillGradeInput> inputs)
    {
        var grades = new List<SkillGrade>(inputs.Count);
        foreach (var input in inputs)
        {
            var gradeResult = SkillGrade.Create(input.Skill, input.Grade);
            if (gradeResult.IsFailure)
                return Result.Failure<IReadOnlyList<SkillGrade>>(gradeResult.Error);

            grades.Add(gradeResult.Value);
        }

        return grades;
    }
}
