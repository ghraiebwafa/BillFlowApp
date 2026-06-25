using BillFlow.Repositories.Billing;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.RefreshTokens;
using BillFlow.Repositories.Security;
using BillFlow.Repositories.Users;
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
        services.AddScoped<IUserSessionRevocationService, UserSessionRevocationService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        return services;
    }
}
