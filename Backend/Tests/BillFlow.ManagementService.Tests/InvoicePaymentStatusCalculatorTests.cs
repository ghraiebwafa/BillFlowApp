using BillFlow.Models.Shared.Enums;
using BillFlow.Shared.Billing;
using Xunit;

namespace BillFlow.ManagementService.Tests;

public sealed class InvoicePaymentStatusCalculatorTests
{
    [Theory]
    [InlineData(InvoiceStatus.Sent, 100, 0, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Sent, 100, 40, InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Sent, 100, 100, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.PartiallyPaid, 100, 100, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Paid, 100, 0, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Draft, 100, 50, InvoiceStatus.Draft)]
    public void Resolve_ReturnsExpectedStatus(
        InvoiceStatus current,
        decimal total,
        decimal paid,
        InvoiceStatus expected) =>
        Assert.Equal(
            expected,
            InvoicePaymentStatusCalculator.Resolve(current, total, paid));

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPartiallyPaidAndPastDue()
    {
        var pastDue = DateTime.UtcNow.Date.AddDays(-3);
        var status = InvoicePaymentStatusCalculator.Resolve(
            InvoiceStatus.PartiallyPaid,
            100m,
            40m,
            pastDue);

        Assert.Equal(InvoiceStatus.Overdue, status);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenSentAndPastDue()
    {
        var pastDue = DateTime.UtcNow.Date.AddDays(-1);
        var status = InvoicePaymentStatusCalculator.Resolve(
            InvoiceStatus.Sent,
            100m,
            0m,
            pastDue);

        Assert.Equal(InvoiceStatus.Overdue, status);
    }
}
