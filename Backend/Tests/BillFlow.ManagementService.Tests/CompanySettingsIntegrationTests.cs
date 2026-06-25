using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class CompanySettingsIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Get_ReturnsNotFound_BeforeSettingsConfigured()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var response = await client.GetAsync("/api/v1.0/billing/CompanySettings/Get");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_ThenGet_ReturnsSettings()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var upsertResponse = await client.PutAsJsonAsync(
            "/api/v1.0/billing/CompanySettings/Upsert",
            new UpsertCompanySettingsRequest
            {
                CompanyName = "Acme Billing Co",
                Address = "100 Main St",
                Country = "US",
                Currency = "USD",
                InvoiceNumberPrefix = "ACME",
                DefaultTaxRate = 15m,
                PaymentTermsDays = 14,
                TimeZone = "America/New_York",
            });

        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1.0/billing/CompanySettings/Get");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var settings = await getResponse.Content.ReadFromJsonAsync<CompanySettingsResponse>(JsonOptions);
        Assert.NotNull(settings);
        Assert.Equal("Acme Billing Co", settings.CompanyName);
        Assert.Equal("ACME", settings.InvoiceNumberPrefix);
        Assert.Equal(15m, settings.DefaultTaxRate);
        Assert.Equal(14, settings.PaymentTermsDays);
    }

    [Fact]
    public async Task CreateInvoice_UsesCompanySettingsDefaults()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        await client.PutAsJsonAsync(
            "/api/v1.0/billing/CompanySettings/Upsert",
            new UpsertCompanySettingsRequest
            {
                CompanyName = "Defaults Co",
                Currency = "EUR",
                InvoiceNumberPrefix = "EUR",
                DefaultTaxRate = 20m,
                PaymentTermsDays = 7,
            });

        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = "Settings Client",
                ContactName = "Settings Contact",
                Email = $"settings-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Invoice/Create",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Service",
                        Quantity = 1,
                        UnitPrice = 100m,
                    },
                ],
            });

        Assert.Equal(HttpStatusCode.Created, createInvoice.StatusCode);
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);
        Assert.StartsWith("EUR-", invoice.InvoiceNumber);
        Assert.Equal(20m, invoice.TaxRate);
        Assert.Equal(20m, invoice.TaxAmount);
        Assert.Equal(120m, invoice.Total);
        Assert.Equal(invoice.InvoiceDate.Date.AddDays(7), invoice.DueDate.Date);
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
                FullName = "Settings Visitor",
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
