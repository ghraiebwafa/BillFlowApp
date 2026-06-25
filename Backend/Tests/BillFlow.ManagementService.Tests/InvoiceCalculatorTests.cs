using BillFlow.ManagementService.Services.Billing;
using BillFlow.Models.Dtos.Billing;
using Xunit;

namespace BillFlow.ManagementService.Tests;

public sealed class InvoiceCalculatorTests
{
    [Fact]
    public void BuildLineItems_CalculatesTotalsCorrectly()
    {
        var (lineItems, subtotal, taxAmount, total) = InvoiceCalculator.BuildLineItems(
        [
            new InvoiceLineItemRequest
            {
                Description = "Design",
                Quantity = 2,
                UnitPrice = 100m,
            },
            new InvoiceLineItemRequest
            {
                Description = "Hosting",
                Quantity = 1,
                UnitPrice = 50m,
            },
        ],
        taxRate: 10m);

        Assert.Equal(2, lineItems.Count);
        Assert.Equal(200m, lineItems[0].LineTotal);
        Assert.Equal(50m, lineItems[1].LineTotal);
        Assert.Equal(250m, subtotal);
        Assert.Equal(25m, taxAmount);
        Assert.Equal(275m, total);
    }
}
