namespace BillFlow.ManagementService.Services.Billing;

public static class BillingInputValidator
{
    public static bool IsNonWhiteSpace(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static bool TryValidateRequiredText(string? value, string fieldName, out string? error)
    {
        if (IsNonWhiteSpace(value))
        {
            error = null;
            return true;
        }

        error = $"{fieldName} is required.";
        return false;
    }
}
