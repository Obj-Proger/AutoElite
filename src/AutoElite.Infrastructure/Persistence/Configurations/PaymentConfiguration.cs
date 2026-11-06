using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Payment"/>; the amount is a complex property of an amount and a currency column.</summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ConfigureAggregateRoot("payments");

        builder.ComplexProperty(payment => payment.Amount, amount => amount.MapMoney());
        builder.Property(payment => payment.Purpose).HasMaxLength(Payment.MaxPurposeLength);
        builder.Property(payment => payment.ExternalReference).HasMaxLength(Payment.MaxExternalReferenceLength);
        builder.Property(payment => payment.Reason).HasMaxLength(Payment.MaxReasonLength);
        builder.HasOne<Contract>().WithMany().HasForeignKey(payment => payment.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(payment => payment.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(payment => payment.RegisteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => payment.RegisteredAtUtc);
        builder.HasIndex(payment => payment.ExternalReference);
    }
}
