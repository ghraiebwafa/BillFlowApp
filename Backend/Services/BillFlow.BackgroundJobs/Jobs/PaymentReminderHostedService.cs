using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Email;

namespace BillFlow.BackgroundJobs.Jobs;

public sealed class PaymentReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Clamp(
            BillFlowEnv.GetInt("PAYMENT_REMINDER_INTERVAL_MINUTES", 360),
            30,
            1440);

        logger.LogInformation(
            "Payment reminder job started. Interval: {IntervalMinutes} minutes.",
            intervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        await RunAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunAsync(stoppingToken);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var candidates = await invoices.GetInvoicesNeedingPaymentRemindersAsync(cancellationToken);
            if (candidates.Count == 0)
                return;

            var sent = 0;
            foreach (var invoice in candidates)
            {
                var settings = invoice.Owner.CompanySettings;
                var clientEmail = invoice.Client.Email;
                if (settings is null || string.IsNullOrWhiteSpace(clientEmail))
                    continue;

                var paid = invoice.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);
                var amountDue = Math.Max(0, invoice.Total - paid);

                var message = AuthEmailComposer.PaymentReminder(
                    clientEmail,
                    invoice.Client.ContactName,
                    settings.CompanyName,
                    invoice.InvoiceNumber,
                    invoice.DueDate,
                    amountDue,
                    settings.Currency);

                var result = await email.SendAsync(message, cancellationToken);
                if (result.Success)
                {
                    await invoices.MarkPaymentReminderSentAsync(invoice.Id, cancellationToken);
                    sent++;
                }
            }

            if (sent > 0)
                logger.LogInformation("Sent {SentCount} payment reminder(s).", sent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Payment reminder job failed.");
        }
    }
}
