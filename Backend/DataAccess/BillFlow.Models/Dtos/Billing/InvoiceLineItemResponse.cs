namespace BillFlow.Models.Dtos.Billing;

public class InvoiceLineItemResponse
{
    public Guid Id { get; set; }

    public Guid? ItemId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }
}
