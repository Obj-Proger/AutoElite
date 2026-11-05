using AutoElite.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="UserAccount"/> on top of the identity mapping: its own table name, and the identifier as a foreign
/// key to the user it belongs to.
/// </summary>
internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.HasOne<User>().WithOne().HasForeignKey<UserAccount>(account => account.Id).OnDelete(DeleteBehavior.Restrict);
    }
}
