using BillFlow.Shared.Extensions;
using Microsoft.OpenApi;

namespace BillFlow.ManagementService.Extensions;

public static class ManagementServiceExtensions
{
    public static IServiceCollection AddBillFlowManagementSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BillFlow Management API",
                Version = "v1",
            });
            options.AddBillFlowJwtSecurity();
        });

        return services;
    }
}
