using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class InvoiceEmailIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task SendInvoice_ThenEmail_ReturnsSuccess_WhenSmtpNotConfigured()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = "Email Client",
                ContactName = "Email Contact",
                Email = $"email-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        await client.PutAsJsonAsync(
            "/api/v1.0/billing/CompanySettings/Upsert",
            new UpsertCompanySettingsRequest
            {
                CompanyName = "Branded BillFlow Co",
                Currency = "USD",
                InvoiceNumberPrefix = "INV",
                BrandColor = "#FF6B00",
                InvoiceFooterNote = "Thank you for your business.",
            });

        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Invoice/Create",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                TaxRate = 10m,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Email service",
                        Quantity = 1,
                        UnitPrice = 120m,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        var sendResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/Send/{invoice.Id}", null);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var emailResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/Email/{invoice.Id}", null);
        Assert.Equal(HttpStatusCode.OK, emailResponse.StatusCode);

        var emailBody = await emailResponse.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
        Assert.NotNull(emailBody);
        Assert.Contains("SMTP", emailBody.Message, StringComparison.OrdinalIgnoreCase);

        var activityResponse = await client.GetAsync("/api/v1.0/billing/Activity/GetRecent?limit=20");
        var events = await activityResponse.Content.ReadFromJsonAsync<List<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.Action == AuditAction.Sent);
    }

    private async Task<string> RegisterAndLoginVisitorAsync()
    {
        var authClient = fixture.AuthFactory.CreateClient();
        var email = $"visitor-{Guid.NewGuid():N}@billflow.test";
        const string password = "SecurePass123!";

        await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/register",
            new RegisterRequest
            {
                FullName = "Email Visitor",
                Email = email,
                Password = password,
                ConfirmPassword = password,
            });

        var loginResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest { Email = email, Password = password });

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);
        return auth.AccessToken;
    }

    private HttpClient CreateManagementClient(string accessToken)
    {
        var httpClient = fixture.ManagementFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }
}
