using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services.Billing;

internal static class PdfBrandPalette
{
    public const string DefaultAccent = "#FF6B00";

    public static string AccentHex(CompanySettingsResponse? issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer?.BrandColor))
            return DefaultAccent;

        var value = issuer.BrandColor.Trim();
        if (!value.StartsWith('#'))
            value = $"#{value}";

        return value.Length == 7 ? value.ToUpperInvariant() : DefaultAccent;
    }
}
