using Microsoft.Extensions.DependencyInjection;

namespace BillFlow.Shared.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddBillFlowEmail(this IServiceCollection services)
    {
        var options = SmtpOptions.FromEnvironment();
        services.AddSingleton(options);

        if (options.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }
}
