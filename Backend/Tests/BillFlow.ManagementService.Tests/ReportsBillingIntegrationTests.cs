using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class ReportsBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ExportSales_ReturnsCsv_WithInvoiceData()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoiceNumber = await CreateSentInvoiceAsync(client);

        var response = await client.GetAsync("/api/v1.0/billing/reports/sales?format=Csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("Invoice Number", csv);
        Assert.Contains(invoiceNumber, csv);
        Assert.Contains("Sales Client", csv);
    }

    [Fact]
    public async Task ExportPayments_ReturnsXlsx()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoice = await CreateSentInvoiceWithTotalAsync(client);

        await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = 110m,
                Method = PaymentMethod.Cash,
            });

        var response = await client.GetAsync("/api/v1.0/billing/reports/payments?format=Xlsx");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]); // P
        Assert.Equal(0x4B, bytes[1]); // K (zip/xlsx)
    }

    [Fact]
    public async Task ExportOutstanding_ReturnsRemainingBalance()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoice = await CreateSentInvoiceWithTotalAsync(client);

        await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = 50m,
                Method = PaymentMethod.Cash,
            });

        var response = await client.GetAsync("/api/v1.0/billing/reports/outstanding?format=Csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csv = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains(invoice.InvoiceNumber, csv);
        Assert.Contains("60.00", csv);
    }

    private async Task<string> CreateSentInvoiceAsync(HttpClient client)
    {
        var invoice = await CreateSentInvoiceWithTotalAsync(client);
        return invoice.InvoiceNumber;
    }

    private async Task<InvoiceDetailResponse> CreateSentInvoiceWithTotalAsync(HttpClient client)
    {
        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Sales Client",
                ContactName = "Reports Contact",
                Email = $"reports-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

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
                        Description = "Report service",
                        Quantity = 1,
                        UnitPrice = 100m,
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
                FullName = "Reports Visitor",
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
