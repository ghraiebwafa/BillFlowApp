namespace BillFlow.Models.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public Guid? ItemId { get; set; }

    public Item? Item { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }
}
