using BillFlow.Models.Shared.Enums;
using BillFlow.ManagementService.Services.Billing;
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
}
