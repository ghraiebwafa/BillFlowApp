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
public sealed class BillingSecurityIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task BillingEndpoints_ReturnUnauthorized_WhenNotAuthenticated()
    {
        var client = fixture.ManagementFactory.CreateClient();

        var endpoints = new[]
        {
            "/api/v1.0/billing/clients",
            "/api/v1.0/billing/items",
            "/api/v1.0/billing/invoices",
            "/api/v1.0/billing/dashboard",
            "/api/v1.0/billing/reports/sales",
        };

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task SuperAdmin_CannotAccessBillingEndpoints()
    {
        var token = await LoginSuperAdminAsync();
        var client = CreateManagementClient(token);

        var response = await client.GetAsync("/api/v1.0/billing/clients");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Visitor_CannotAccessAnotherOwnersClient()
    {
        var ownerAToken = await RegisterAndLoginVisitorAsync();
        var ownerAClient = CreateManagementClient(ownerAToken);

        var createResponse = await ownerAClient.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Owner A Client",
                ContactName = "Owner A",
                Email = $"owner-a-{Guid.NewGuid():N}@billflow.test",
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(created);

        var ownerBToken = await RegisterAndLoginVisitorAsync();
        var ownerBClient = CreateManagementClient(ownerBToken);

        var getResponse = await ownerBClient.GetAsync($"/api/v1.0/billing/clients/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Visitor_CannotAccessAnotherOwnersInvoice()
    {
        var ownerAToken = await RegisterAndLoginVisitorAsync();
        var ownerAClient = CreateManagementClient(ownerAToken);
        var invoiceId = await CreateSentInvoiceAsync(ownerAClient);

        var ownerBToken = await RegisterAndLoginVisitorAsync();
        var ownerBClient = CreateManagementClient(ownerBToken);

        var getResponse = await ownerBClient.GetAsync($"/api/v1.0/billing/invoices/{invoiceId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var pdfResponse = await ownerBClient.GetAsync($"/api/v1.0/billing/invoices/{invoiceId}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, pdfResponse.StatusCode);
    }

    [Fact]
    public async Task Visitor_CannotRecordPaymentOnAnotherOwnersInvoice()
    {
        var ownerAToken = await RegisterAndLoginVisitorAsync();
        var ownerAClient = CreateManagementClient(ownerAToken);
        var invoiceId = await CreateSentInvoiceAsync(ownerAClient);

        var ownerBToken = await RegisterAndLoginVisitorAsync();
        var ownerBClient = CreateManagementClient(ownerBToken);

        var response = await ownerBClient.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoiceId,
                Amount = 10m,
                Method = PaymentMethod.Cash,
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateClient_ReturnsBadRequest_ForWhitespaceOnlyCompanyName()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "   ",
                ContactName = "Valid Contact",
                Email = $"whitespace-{Guid.NewGuid():N}@billflow.test",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateClient_AllowsReusingEmail_AfterSoftDelete()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var email = $"reuse-{Guid.NewGuid():N}@billflow.test";

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "First Client",
                ContactName = "First Contact",
                Email = email,
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(created);

        var deleteResponse = await client.DeleteAsync($"/api/v1.0/billing/clients/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var recreateResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Second Client",
                ContactName = "Second Contact",
                Email = email,
            });
        Assert.Equal(HttpStatusCode.Created, recreateResponse.StatusCode);
    }

    [Fact]
    public async Task MarkPaid_CreatesPaymentRecord_ForDashboardRevenue()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoiceId = await CreateSentInvoiceAsync(client, total: 110m);

        var markPaidResponse = await client.PostAsync($"/api/v1.0/billing/invoices/{invoiceId}/mark-paid", null);
        Assert.Equal(HttpStatusCode.OK, markPaidResponse.StatusCode);

        var dashboardResponse = await client.GetAsync("/api/v1.0/billing/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);

        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(110m, dashboard.TotalRevenue);
    }

    [Fact]
    public async Task DownloadPdf_ReturnsBadRequest_ForDraftInvoice()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Draft PDF Client",
                ContactName = "Draft Contact",
                Email = $"draft-pdf-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Draft service",
                        Quantity = 1,
                        UnitPrice = 50m,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        var response = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Visitor_CannotManageAnotherOwnersShareLink()
    {
        var ownerAToken = await RegisterAndLoginVisitorAsync();
        var ownerAClient = CreateManagementClient(ownerAToken);
        var invoiceId = await CreateSentInvoiceAsync(ownerAClient);

        var shareResponse = await ownerAClient.PostAsync($"/api/v1.0/billing/invoices/{invoiceId}/share-link", null);
        Assert.Equal(HttpStatusCode.Created, shareResponse.StatusCode);

        var ownerBToken = await RegisterAndLoginVisitorAsync();
        var ownerBClient = CreateManagementClient(ownerBToken);

        var revokeResponse = await ownerBClient.DeleteAsync($"/api/v1.0/billing/invoices/{invoiceId}/share-link");
        Assert.Equal(HttpStatusCode.NotFound, revokeResponse.StatusCode);

        var generateResponse = await ownerBClient.PostAsync($"/api/v1.0/billing/invoices/{invoiceId}/share-link", null);
        Assert.Equal(HttpStatusCode.NotFound, generateResponse.StatusCode);
    }

    [Fact]
    public async Task ExportTaxes_ReturnsCsv()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        await CreateSentInvoiceAsync(client, total: 110m);

        var response = await client.GetAsync("/api/v1.0/billing/reports/taxes?format=Csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<Guid> CreateSentInvoiceAsync(HttpClient client, decimal total = 110m)
    {
        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Security Client",
                ContactName = "Security Contact",
                Email = $"security-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        var unitPrice = total / 1.1m;
        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                TaxRate = 10m,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Security service",
                        Quantity = 1,
                        UnitPrice = unitPrice,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        await client.PostAsync($"/api/v1.0/billing/invoices/{invoice.Id}/send", null);
        return invoice.Id;
    }

    private async Task<string> LoginSuperAdminAsync()
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
        return auth.AccessToken;
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
                FullName = "Security Visitor",
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
