namespace BillFlow.Repositories;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
