namespace AutoElite.Domain.Common;

/// <summary>
/// Base class for entities whose creation and last modification are recorded.
/// </summary>
/// <remarks>
/// The audit properties have private setters and are never changed by domain code: infrastructure fills
/// them in when the unit of work is saved, using the current time and the current user. Until then they
/// hold their default values.
/// </remarks>
public abstract class AuditableEntity : BaseEntity, IAuditable
{
    /// <param name="id">The identifier, assigned by the aggregate's own factory method.</param>
    protected AuditableEntity(Guid id) : base(id)
    {
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    protected AuditableEntity()
    {
    }

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; private set; }

    /// <inheritdoc />
    public Guid? CreatedByUserId { get; private set; }

    /// <inheritdoc />
    public DateTime? ModifiedAtUtc { get; private set; }

    /// <inheritdoc />
    public Guid? ModifiedByUserId { get; private set; }
}
