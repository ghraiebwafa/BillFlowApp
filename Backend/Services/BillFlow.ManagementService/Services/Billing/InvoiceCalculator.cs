using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;

namespace BillFlow.ManagementService.Services.Billing;

public static class InvoiceCalculator
{
    public static (IReadOnlyList<InvoiceLineItem> LineItems, decimal Subtotal, decimal TaxAmount, decimal Total)
        BuildLineItems(
            IReadOnlyList<InvoiceLineItemRequest> requests,
            decimal taxRate)
    {
        var lineItems = new List<InvoiceLineItem>();
        var subtotal = 0m;

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var lineTotal = Math.Round(request.Quantity * request.UnitPrice, 2, MidpointRounding.AwayFromZero);
            subtotal += lineTotal;

            lineItems.Add(new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                ItemId = request.ItemId,
                Description = request.Description.Trim(),
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                LineTotal = lineTotal,
                SortOrder = index,
            });
        }

        subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        var taxAmount = Math.Round(subtotal * taxRate / 100m, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(subtotal + taxAmount, 2, MidpointRounding.AwayFromZero);

        return (lineItems, subtotal, taxAmount, total);
    }
}
