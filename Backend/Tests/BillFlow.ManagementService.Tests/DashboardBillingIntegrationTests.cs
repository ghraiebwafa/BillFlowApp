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
public sealed class DashboardBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task GetSummary_ReturnsExpectedMetrics()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await CreateClientAsync(client, "Dashboard Client Co");

        var invoice = await CreateSentInvoiceAsync(client, billingClient.Id, 200m, 10m);

        await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = 110m,
                Method = PaymentMethod.BankTransfer,
            });

        var response = await client.GetAsync("/api/v1.0/billing/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.TotalInvoices);
        Assert.Equal(1, dashboard.ActiveClientsCount);
        Assert.Equal(110m, dashboard.TotalRevenue);
        Assert.Equal(110m, dashboard.MonthlyIncome);
        Assert.Equal(110m, dashboard.PendingPaymentsAmount);
        Assert.True(dashboard.RevenueByMonth.Count >= 1);
        Assert.Contains(dashboard.InvoicesByStatus, s => s.Status == InvoiceStatus.PartiallyPaid && s.Count == 1);
        Assert.Contains(dashboard.PaymentsByMethod, p => p.Method == PaymentMethod.BankTransfer && p.Amount == 110m);
        Assert.Single(dashboard.TopClients);
        Assert.Equal(billingClient.Id, dashboard.TopClients[0].ClientId);
        Assert.Equal(110m, dashboard.TopClients[0].Revenue);
    }

    [Fact]
    public async Task SuperAdmin_CannotAccessDashboard()
    {
        var authClient = fixture.AuthFactory.CreateClient();
        var loginResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest
            {
                Email = fixture.SuperAdminEmailAddress,
                Password = fixture.SuperAdminPasswordValue,
            });

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);

        var managementClient = fixture.ManagementFactory.CreateClient();
        managementClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await managementClient.GetAsync("/api/v1.0/billing/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<ClientResponse> CreateClientAsync(HttpClient client, string companyName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = companyName,
                ContactName = "Dashboard Contact",
                Email = $"dash-{Guid.NewGuid():N}@billflow.test",
            });

        var created = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    private async Task<InvoiceDetailResponse> CreateSentInvoiceAsync(
        HttpClient client,
        Guid clientId,
        decimal unitPrice,
        decimal taxRate)
    {
        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = clientId,
                TaxRate = taxRate,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Dashboard service",
                        Quantity = 1,
                        UnitPrice = unitPrice,
                    },
                ],
            });

        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        await client.PostAsync($"/api/v1.0/billing/invoices/{invoice.Id}/send", null);

        var sentResponse = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}");
        return (await sentResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions))!;
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
                FullName = "Dashboard Visitor",
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
