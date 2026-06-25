namespace BillFlow.Models.Dtos.Billing;

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
