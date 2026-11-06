namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds payments; a payment has no child entities.</summary>
internal sealed class PaymentRepository(AutoEliteDbContext dbContext) : Repository<Payment>(dbContext);
