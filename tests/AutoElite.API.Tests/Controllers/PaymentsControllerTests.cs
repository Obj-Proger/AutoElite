using System.Globalization;
using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The payments towards a contract, from recording them to the report of the school.</summary>
[Collection(nameof(ApiCollection))]
public sealed class PaymentsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    [Fact]
    public async Task Manager_RecordsConfirmsRefundsAndCancelsPayments_WhichTheStudentSees()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentUser.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });
        var contract = $"/api/v1/contracts/{contractId}";

        var cash = new { Amount = 20000m, Method = PaymentMethod.Cash, Purpose = "Первый взнос" };
        using var recorded = await admin.SendJsonAsync(HttpMethod.Post, $"{contract}/payments", cash);
        var cashId = (await recorded.ReadJsonAsync()).GetProperty("id").GetGuid();
        var transferId = await admin.CreateAsync(
            $"{contract}/payments", new { Amount = 25000m, Method = PaymentMethod.BankTransfer, Purpose = "Второй взнос" });
        using var confirmed = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/payments/{cashId}/confirm");
        using var referenced = await admin.SendJsonAsync(
            HttpMethod.Put, $"/api/v1/payments/{transferId}/reference", new { ExternalReference = "П/П 123" });
        var afterConfirmation = await admin.GetJsonAsync(contract);
        var own = await student.GetJsonAsync("/api/v1/me/payments");

        recorded.StatusCode.Should().Be(HttpStatusCode.Created);
        recorded.Headers.Location!.AbsolutePath.Should().Be($"{contract}/payments");
        confirmed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        referenced.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterConfirmation.GetProperty("paidAmount").GetDecimal().Should().Be(20000m);
        afterConfirmation.GetProperty("debtAmount").GetDecimal().Should().Be(25000m);
        own.EnumerateArray().Select(payment => payment.GetProperty("id").GetGuid()).Should().BeEquivalentTo(new[] { cashId, transferId });

        using var refunded = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/payments/{cashId}/refund", new { Reason = "Отказ" });
        using var cancelled = await admin.SendJsonAsync(
            HttpMethod.Post, $"/api/v1/payments/{transferId}/cancel", new { Reason = "Не поступил" });
        var history = await student.GetJsonAsync($"{contract}/payments");
        var afterRefund = await admin.GetJsonAsync(contract);
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var number = Uri.EscapeDataString(afterRefund.GetProperty("number").GetString()!);
        var report = await admin.GetJsonAsync($"/api/v1/payments?from={today}&to={today}&search={number}");

        refunded.StatusCode.Should().Be(HttpStatusCode.NoContent);
        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var statuses = history.EnumerateArray().ToDictionary(
            payment => payment.GetProperty("id").GetGuid(), payment => payment.GetProperty("status").GetString());
        statuses[cashId].Should().Be(nameof(PaymentStatus.Refunded));
        statuses[transferId].Should().Be(nameof(PaymentStatus.Cancelled));
        afterRefund.GetProperty("paidAmount").GetDecimal().Should().Be(0m);
        report.GetProperty("page").GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Student_CannotReadThePaymentsOfTheSchool()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);

        using var response = await student.GetAsync("/api/v1/payments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
