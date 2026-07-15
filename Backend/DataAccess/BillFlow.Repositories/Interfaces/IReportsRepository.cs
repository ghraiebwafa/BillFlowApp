using BillFlow.Models.Dtos.Billing;

namespace BillFlow.Repositories.Interfaces;

public interface IReportsRepository
{
    Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentReportRow>> GetPaymentsAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutstandingReportRow>> GetOutstandingAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxReportRow>> GetTaxesAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
