using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BillFlow.Shared.Extensions;

public static class SwaggerExtensions
{
    public static SwaggerGenOptions AddBillFlowJwtSecurity(this SwaggerGenOptions options)
    {
        const string schemeId = "Bearer";

        options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Bearer token. Example: Bearer {your access token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = [],
        });

        return options;
    }
}
