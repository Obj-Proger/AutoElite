using AutoElite.Application.Leads.AddLeadNote;
using AutoElite.Application.Leads.CreateLead;
using AutoElite.Application.Leads.MarkLeadLost;
using AutoElite.Application.Leads.StartLeadWork;
using AutoElite.Application.Payments.RegisterPayment;

namespace AutoElite.API.DemoData;

/// <content>
/// The work of the office of the demo school: the leads of the last days, new from the website and the phone, one the
/// manager works on with a note and one lost to another school, and a payment of the second student at the desk that
/// waits for its money.
/// </content>
internal sealed partial class DemoDataSeeder
{
    private void PlanOffice(
        DemoCaller administrator,
        DemoCaller manager,
        Guid branchId,
        List<Guid> contractIds,
        DateOnly today,
        TimeZoneInfo zone,
        List<(DateTime At, Func<Task> Step)> past,
        CancellationToken cancellationToken)
    {
        // A lead created by the administrator waits for a manager; one created by the manager is theirs.
        Task<Guid> Lead(DemoCaller caller, string name, string? phone, string? email, LeadSource source, string? comment) =>
            SendAsync(
                caller,
                new CreateLeadCommand(name, phone, email, source, LicenseCategory.B, comment, branchId, null),
                cancellationToken);

        Plan(past, UtcOf(today.AddDays(-2), 14, 10, zone), () => Lead(
            administrator, "Ковалёва Ирина", "+79990000021", null, LeadSource.Website,
            "Хочу учиться на автомате, удобно по вечерам после работы."));
        Plan(past, UtcOf(today.AddDays(-1), 11, 30, zone), () => Lead(
            administrator, "Морозов Павел", null, "pavel.morozov@example.com", LeadSource.Website, null));
        Plan(past, UtcOf(today.AddDays(-1), 18, 45, zone), () => Lead(
            manager, "Егорова Светлана", "+79990000022", null, LeadSource.PhoneCall, "Звонила, спрашивала про рассрочку."));

        Plan(past, UtcOf(today.AddDays(-6), 10, 0, zone), async () =>
        {
            var leadId = await Lead(manager, "Лебедев Артём", "+79990000023", null, LeadSource.Referral, "Посоветовал Смирнов Олег.");
            await SendAsync(manager, new StartLeadWorkCommand(leadId), cancellationToken);
            await SendAsync(
                manager, new AddLeadNoteCommand(leadId, "Перезвонить в пятницу: хочет начать с первого числа."), cancellationToken);
        });
        Plan(past, UtcOf(today.AddDays(-10), 12, 0, zone), async () =>
        {
            var leadId = await Lead(administrator, "Белов Игорь", "+79990000024", null, LeadSource.Advertising, null);
            await SendAsync(manager, new StartLeadWorkCommand(leadId), cancellationToken);
            await SendAsync(manager, new MarkLeadLostCommand(leadId, "Выбрал автошколу ближе к дому."), cancellationToken);
        });

        Plan(past, UtcOf(today.AddDays(-1), 15, 0, zone), () => SendAsync(
            manager,
            new RegisterPaymentCommand(contractIds[1], 5000m, PaymentMethod.BankTransfer, "Второй взнос", ConfirmNow: false),
            cancellationToken));
    }
}
