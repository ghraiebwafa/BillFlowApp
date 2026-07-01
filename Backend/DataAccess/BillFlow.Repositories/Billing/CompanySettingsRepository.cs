using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class CompanySettingsRepository(BillFlowDbContext db) : ICompanySettingsRepository
{
    public Task<CompanySettings?> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        db.CompanySettings.FirstOrDefaultAsync(s => s.OwnerId == ownerId, cancellationToken);

    public async Task<CompanySettings> UpsertAsync(
        CompanySettings settings,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.OwnerId == settings.OwnerId, cancellationToken);

        if (existing is null)
        {
            settings.CreatedAt = DateTime.UtcNow;
            db.CompanySettings.Add(settings);
        }
        else
        {
            existing.CompanyName = settings.CompanyName;
            existing.Address = settings.Address;
            existing.Country = settings.Country;
            existing.TaxNumber = settings.TaxNumber;
            existing.PhoneNumber = settings.PhoneNumber;
            existing.Email = settings.Email;
            existing.Currency = settings.Currency;
            existing.InvoiceNumberPrefix = settings.InvoiceNumberPrefix;
            existing.DefaultTaxRate = settings.DefaultTaxRate;
            existing.PaymentTermsDays = settings.PaymentTermsDays;
            existing.TimeZone = settings.TimeZone;
            existing.BrandColor = settings.BrandColor;
            existing.InvoiceFooterNote = settings.InvoiceFooterNote;
            existing.UpdatedAt = DateTime.UtcNow;
            settings = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
