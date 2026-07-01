using BillFlow.Repositories.Billing;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.RefreshTokens;
using BillFlow.Repositories.Security;
using BillFlow.Repositories.Users;
using BillFlow.Shared.Security;
using Microsoft.Extensions.DependencyInjection;

namespace BillFlow.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddBillFlowRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IReportsRepository, ReportsRepository>();
        services.AddScoped<ICompanySettingsRepository, CompanySettingsRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IUserSessionRevocationService, UserSessionRevocationService>();
        services.AddScoped<ITokenSessionService, PersistedTokenSessionService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        return services;
    }
}
