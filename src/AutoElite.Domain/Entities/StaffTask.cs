using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A task for a staff member with a deadline (FR-12.1..FR-12.4), optionally linked to another object such as a lead,
/// and optionally repeating.
/// </summary>
/// <remarks>
/// <para>
/// Completing a repeating task returns its next occurrence, due one period after the previous deadline (and after the
/// current time); the caller saves both. Reopening a task does not remove the occurrence already created.
/// </para>
/// <para>
/// A background job calls <see cref="MarkOverdue"/> for open tasks past their deadline; it raises
/// <see cref="StaffTaskOverdueDomainEvent"/> once, and moving the deadline allows a new reminder.
/// </para>
/// </remarks>
public sealed class StaffTask : AuditableEntity
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Gets the description, if any.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the identifier of the staff member responsible for the task.</summary>
    public Guid AssigneeId { get; private set; }

    /// <summary>Gets the UTC deadline.</summary>
    public DateTime DueAtUtc { get; private set; }

    /// <summary>Gets the object the task is about, if any.</summary>
    public EntityReference? Subject { get; private set; }

    /// <summary>Gets how often the task repeats.</summary>
    public TaskRecurrence Recurrence { get; private set; }

    /// <summary>Gets the status.</summary>
    public StaffTaskStatus Status { get; private set; }

    /// <summary>Gets the UTC time the task was last completed, or <see langword="null"/> if it is open.</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the assignee was reminded that the task is overdue, if they were for the current deadline.</summary>
    public DateTime? OverdueNotifiedAtUtc { get; private set; }

    private StaffTask(
        Guid id, string title, string? description, Guid assigneeId, DateTime dueAtUtc, EntityReference? subject, TaskRecurrence recurrence)
        : base(id)
    {
        Title = title;
        Description = description;
        AssigneeId = assigneeId;
        DueAtUtc = dueAtUtc;
        Subject = subject;
        Recurrence = recurrence;
        Status = StaffTaskStatus.New;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private StaffTask()
    {
    }

    /// <summary>Creates a task and raises <see cref="StaffTaskAssignedDomainEvent"/> so the assignee is notified.</summary>
    /// <param name="title">The title. Required.</param>
    /// <param name="description">The description, if any.</param>
    /// <param name="assigneeId">The staff member responsible; checking the role is up to the caller.</param>
    /// <param name="dueAtUtc">The deadline; it must be in the future.</param>
    /// <param name="subject">The object the task is about, if any.</param>
    /// <param name="recurrence">How often the task repeats.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The task, or the first validation failure encountered.</returns>
    public static Result<StaffTask> Create(
        string? title,
        string? description,
        Guid assigneeId,
        DateTime dueAtUtc,
        EntityReference? subject,
        TaskRecurrence recurrence,
        DateTime utcNow)
    {
        var textResult = ValidateText(title, description);
        if (textResult.IsFailure)
            return Result.Failure<StaffTask>(textResult.Error);

        var assigneeResult = Guard.NotEmpty(assigneeId, DomainErrors.StaffTask.AssigneeIdEmpty);
        if (assigneeResult.IsFailure)
            return Result.Failure<StaffTask>(assigneeResult.Error);

        if (dueAtUtc <= utcNow)
            return Result.Failure<StaffTask>(DomainErrors.StaffTask.DueInPast);

        var recurrenceResult = Guard.DefinedEnum(recurrence, DomainErrors.StaffTask.RecurrenceNotDefined);
        if (recurrenceResult.IsFailure)
            return Result.Failure<StaffTask>(recurrenceResult.Error);

        var (validTitle, validDescription) = textResult.Value;
        var task = new StaffTask(Guid.CreateVersion7(), validTitle, validDescription, assigneeId, dueAtUtc, subject, recurrence);
        task.RaiseDomainEvent(new StaffTaskAssignedDomainEvent(task.Id, assigneeId, dueAtUtc, utcNow));

        return task;
    }

    /// <summary>Changes the title, description, deadline and linked object of an open task.</summary>
    /// <returns>A failure if the task is done, or the first validation failure encountered.</returns>
    public Result UpdateDetails(string? title, string? description, DateTime dueAtUtc, EntityReference? subject, DateTime utcNow)
    {
        if (Status == StaffTaskStatus.Done)
            return Result.Failure(DomainErrors.StaffTask.AlreadyDone);

        var textResult = ValidateText(title, description);
        if (textResult.IsFailure)
            return Result.Failure(textResult.Error);

        if (dueAtUtc != DueAtUtc && dueAtUtc <= utcNow)
            return Result.Failure(DomainErrors.StaffTask.DueInPast);

        (Title, Description) = textResult.Value;
        Subject = subject;

        if (dueAtUtc != DueAtUtc)
        {
            DueAtUtc = dueAtUtc;
            OverdueNotifiedAtUtc = null;
        }

        return Result.Success();
    }

    /// <summary>Gives an open task to another staff member and raises <see cref="StaffTaskAssignedDomainEvent"/>.</summary>
    /// <returns>A failure if the task is done, the identifier is empty, or it is the current assignee.</returns>
    public Result Reassign(Guid assigneeId, DateTime utcNow)
    {
        if (Status == StaffTaskStatus.Done)
            return Result.Failure(DomainErrors.StaffTask.AlreadyDone);

        var assigneeResult = Guard.NotEmpty(assigneeId, DomainErrors.StaffTask.AssigneeIdEmpty);
        if (assigneeResult.IsFailure)
            return Result.Failure(assigneeResult.Error);

        if (assigneeId == AssigneeId)
            return Result.Failure(DomainErrors.StaffTask.AlreadyAssigned);

        AssigneeId = assigneeId;
        RaiseDomainEvent(new StaffTaskAssignedDomainEvent(Id, assigneeId, DueAtUtc, utcNow));

        return Result.Success();
    }

    /// <summary>Starts work on a new task.</summary>
    /// <returns>A failure if the task is not new.</returns>
    public Result Start()
    {
        if (Status != StaffTaskStatus.New)
            return Result.Failure(DomainErrors.StaffTask.NotNew);

        Status = StaffTaskStatus.InProgress;

        return Result.Success();
    }

    /// <summary>Completes an open task. For a repeating task, returns its next occurrence for the caller to save.</summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The next occurrence, or <see langword="null"/> for a task that does not repeat; a failure if the task is already done.</returns>
    public Result<StaffTask?> Complete(DateTime utcNow)
    {
        if (Status == StaffTaskStatus.Done)
            return Result.Failure<StaffTask?>(DomainErrors.StaffTask.AlreadyDone);

        Status = StaffTaskStatus.Done;
        CompletedAtUtc = utcNow;

        if (Recurrence == TaskRecurrence.None)
            return Result.Success<StaffTask?>(null);

        var next = new StaffTask(
            Guid.CreateVersion7(), Title, Description, AssigneeId, NextDueAfter(utcNow), Subject, Recurrence);
        next.RaiseDomainEvent(new StaffTaskAssignedDomainEvent(next.Id, AssigneeId, next.DueAtUtc, utcNow));

        return Result.Success<StaffTask?>(next);
    }

    /// <summary>Reopens a done task, returning it to work.</summary>
    /// <returns>A failure if the task is not done.</returns>
    public Result Reopen()
    {
        if (Status != StaffTaskStatus.Done)
            return Result.Failure(DomainErrors.StaffTask.NotDone);

        Status = StaffTaskStatus.InProgress;
        CompletedAtUtc = null;

        return Result.Success();
    }

    /// <summary>
    /// Records that an open task is past its deadline and raises <see cref="StaffTaskOverdueDomainEvent"/>, once per
    /// deadline.
    /// </summary>
    /// <returns>A failure if the task is done, not yet overdue, or the assignee was already reminded.</returns>
    public Result MarkOverdue(DateTime utcNow)
    {
        if (Status == StaffTaskStatus.Done)
            return Result.Failure(DomainErrors.StaffTask.AlreadyDone);

        if (utcNow <= DueAtUtc)
            return Result.Failure(DomainErrors.StaffTask.NotOverdue);

        if (OverdueNotifiedAtUtc is not null)
            return Result.Failure(DomainErrors.StaffTask.OverdueAlreadyNotified);

        OverdueNotifiedAtUtc = utcNow;
        RaiseDomainEvent(new StaffTaskOverdueDomainEvent(Id, AssigneeId, DueAtUtc, utcNow));

        return Result.Success();
    }

    private DateTime NextDueAfter(DateTime utcNow)
    {
        var due = DueAtUtc;

        do
        {
            due = Recurrence switch
            {
                TaskRecurrence.Daily => due.AddDays(1),
                TaskRecurrence.Weekly => due.AddDays(7),
                _ => due.AddMonths(1)
            };
        }
        while (due <= utcNow);

        return due;
    }

    private static Result<(string Title, string? Description)> ValidateText(string? title, string? description)
    {
        var titleResult = Guard.RequiredText(title, MaxTitleLength, DomainErrors.StaffTask.TitleEmpty, DomainErrors.StaffTask.TitleTooLong);
        if (titleResult.IsFailure)
            return Result.Failure<(string, string?)>(titleResult.Error);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.StaffTask.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<(string, string?)>(descriptionResult.Error);

        return Result.Success((titleResult.Value, descriptionResult.Value));
    }
}
