using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Contract"/>: the number is unique; the debt is computed from the price and the amount paid and is
/// not stored, while the quality score is, because it depends on the window the school had set when it was computed.
/// </summary>
internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    /// <summary>The digits of practice hours, enough for <see cref="Course.MaxHours"/> with two decimals.</summary>
    private const int HoursPrecision = 7;

    /// <summary>The digits of a grade or the quality score, from 1.00 to 5.00.</summary>
    private const int GradePrecision = 3;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ConfigureAggregateRoot("contracts");

        builder.Property(contract => contract.Number).HasMaxLength(Contract.MaxNumberLength);
        builder.HasIndex(contract => contract.Number).IsUnique();
        builder.Property(contract => contract.TerminationReason).HasMaxLength(Contract.MaxTerminationReasonLength);
        builder.ComplexProperty(contract => contract.Price, price => price.MapMoney());
        builder.ComplexProperty(contract => contract.PaidAmount, paid => paid.MapMoney());
        builder.Property(contract => contract.CompletedPracticeHours).HasPrecision(HoursPrecision, Contract.GradeDecimals);
        builder.Property(contract => contract.QualityScore).HasPrecision(GradePrecision, Contract.GradeDecimals);
        builder.PrimitiveCollection(contract => contract.RecentGrades)
            .HasField("_recentGrades")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType(grade => grade.HasPrecision(GradePrecision, Contract.GradeDecimals));

        builder.HasOne<User>().WithMany().HasForeignKey(contract => contract.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Course>().WithMany().HasForeignKey(contract => contract.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(contract => contract.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(contract => contract.StartDate);
    }
}

/// <summary>Maps <see cref="ContractNumberCounter"/>, the last contract number given out in each year.</summary>
internal sealed class ContractNumberCounterConfiguration : IEntityTypeConfiguration<ContractNumberCounter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContractNumberCounter> builder)
    {
        builder.ToTable("contract_number_counters");
        builder.HasKey(counter => counter.Year);
        builder.Property(counter => counter.Year).ValueGeneratedNever();
    }
}
