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
public sealed class InvoiceBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task BusinessOwner_CanManageInvoices_EndToEnd()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await CreateClientAsync(client);
        var item = await CreateItemAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = billingClient.Id,
                TaxRate = 10m,
                Notes = "Thank you for your business",
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        ItemId = item.Id,
                        Description = "Consulting",
                        Quantity = 2,
                        UnitPrice = 100m,
                    },
                    new InvoiceLineItemRequest
                    {
                        Description = "Setup fee",
                        Quantity = 1,
                        UnitPrice = 50m,
                    },
                ],
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.StartsWith("INV-", created.InvoiceNumber);
        Assert.Equal(InvoiceStatus.Draft, created.Status);
        Assert.Equal(250m, created.Subtotal);
        Assert.Equal(25m, created.TaxAmount);
        Assert.Equal(275m, created.Total);
        Assert.Equal(2, created.LineItems.Count);

        var sendResponse = await client.PostAsync($"/api/v1.0/billing/invoices/{created.Id}/send", null);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var sent = await sendResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(sent);
        Assert.Equal(InvoiceStatus.Sent, sent.Status);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1.0/billing/invoices/{created.Id}",
            new UpdateInvoiceRequest
            {
                ClientId = billingClient.Id,
                InvoiceDate = created.InvoiceDate,
                DueDate = created.DueDate,
                TaxRate = 10m,
                LineItems = created.LineItems.Select(l => new InvoiceLineItemRequest
                {
                    ItemId = l.ItemId,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                }).ToList(),
            });
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var paidResponse = await client.PostAsync($"/api/v1.0/billing/invoices/{created.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.OK, paidResponse.StatusCode);

        var paid = await paidResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(paid);
        Assert.Equal(InvoiceStatus.Paid, paid.Status);

        var duplicateResponse = await client.PostAsync($"/api/v1.0/billing/invoices/{created.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, duplicateResponse.StatusCode);

        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(duplicate);
        Assert.Equal(InvoiceStatus.Draft, duplicate.Status);
        Assert.NotEqual(created.InvoiceNumber, duplicate.InvoiceNumber);

        var listResponse = await client.GetAsync("/api/v1.0/billing/invoices?status=Draft");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var summaries = await listResponse.Content.ReadFromJsonAsync<List<InvoiceSummaryResponse>>(JsonOptions);
        Assert.NotNull(summaries);
        Assert.Contains(summaries, i => i.Id == duplicate.Id);
    }

    [Fact]
    public async Task CreateInvoice_ReturnsBadRequest_WhenDueDateBeforeInvoiceDate()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var billingClient = await CreateClientAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = billingClient.Id,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(-1),
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<ClientResponse> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Invoice Client Co",
                ContactName = "Sam Client",
                Email = $"invoice-client-{Guid.NewGuid():N}@billflow.test",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    private async Task<ItemResponse> CreateItemAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/items",
            new CreateItemRequest
            {
                Name = "Consulting Hour",
                UnitPrice = 100m,
                Unit = "hour",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ItemResponse>(JsonOptions);
        Assert.NotNull(created);
        return created;
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
                FullName = "Invoice Test Visitor",
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
