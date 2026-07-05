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
public sealed class CustomerPortalIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ShareLink_GenerateAndViewThroughPortal()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = "Portal Client",
                ContactName = "Portal Contact",
                Email = $"portal-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        await client.PutAsJsonAsync(
            "/api/v1.0/billing/CompanySettings/Upsert",
            new UpsertCompanySettingsRequest
            {
                CompanyName = "Portal Test Co",
                Currency = "EUR",
                InvoiceNumberPrefix = "PRT",
                BrandColor = "#3B82F6",
                InvoiceFooterNote = "Thank you for choosing Portal Test Co.",
            });

        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Invoice/Create",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                TaxRate = 20m,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Portal consultation",
                        Quantity = 3,
                        UnitPrice = 200m,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        var sendResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/Send/{invoice.Id}", null);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        // Draft invoices can't be shared
        var draftInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Invoice/Create",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                TaxRate = 0m,
                LineItems = [new InvoiceLineItemRequest { Description = "Draft item", Quantity = 1, UnitPrice = 10m }],
            });
        var draftResult = await draftInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(draftResult);
        var draftShareResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/ShareLink/{draftResult.Id}", null);
        Assert.Equal(HttpStatusCode.BadRequest, draftShareResponse.StatusCode);

        // Generate share link
        var shareResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/ShareLink/{invoice.Id}", null);
        Assert.Equal(HttpStatusCode.Created, shareResponse.StatusCode);
        var shareLink = await shareResponse.Content.ReadFromJsonAsync<ShareLinkResponse>(JsonOptions);
        Assert.NotNull(shareLink);
        Assert.False(string.IsNullOrEmpty(shareLink.Token));

        // Second call indicates link is already active (token not returned again)
        var shareResponse2 = await client.PostAsync($"/api/v1.0/billing/Invoice/ShareLink/{invoice.Id}", null);
        Assert.Equal(HttpStatusCode.OK, shareResponse2.StatusCode);
        var shareLink2 = await shareResponse2.Content.ReadFromJsonAsync<ShareLinkResponse>(JsonOptions);
        Assert.NotNull(shareLink2);
        Assert.True(shareLink2.AlreadyActive);
        Assert.Null(shareLink2.Token);

        // View invoice via public portal (no auth header)
        var portalClient = fixture.ManagementFactory.CreateClient();
        var portalResponse = await portalClient.GetAsync($"/api/v1.0/portal/{shareLink.Token}");
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portalInvoice = await portalResponse.Content.ReadFromJsonAsync<PublicInvoiceResponse>(JsonOptions);
        Assert.NotNull(portalInvoice);
        Assert.Equal(invoice.InvoiceNumber, portalInvoice.InvoiceNumber);
        Assert.Equal(InvoiceStatus.Sent, portalInvoice.Status);
        Assert.NotNull(portalInvoice.Issuer);
        Assert.Equal("Portal Test Co", portalInvoice.Issuer.CompanyName);
        Assert.Equal("#3B82F6", portalInvoice.Issuer.BrandColor);

        // Download PDF via portal
        var pdfResponse = await portalClient.GetAsync($"/api/v1.0/portal/{shareLink.Token}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(pdfBytes.Length > 100);

        // Invalid token returns 404
        var invalidResponse = await portalClient.GetAsync("/api/v1.0/portal/invalid-token-xyz");
        Assert.Equal(HttpStatusCode.NotFound, invalidResponse.StatusCode);

        // Revoke share link
        var revokeResponse = await client.DeleteAsync($"/api/v1.0/billing/Invoice/ShareLink/{invoice.Id}");
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        // Revoked token no longer works
        var revokedPortalResponse = await portalClient.GetAsync($"/api/v1.0/portal/{shareLink.Token}");
        Assert.Equal(HttpStatusCode.NotFound, revokedPortalResponse.StatusCode);

        // Verify audit trail includes portal events
        var activityResponse = await client.GetAsync("/api/v1.0/billing/Activity/GetRecent?limit=50");
        var events = await activityResponse.Content.ReadFromJsonAsync<List<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.Action == AuditAction.ShareLinkCreated);
        Assert.Contains(events, e => e.Action == AuditAction.PortalViewed);
        Assert.Contains(events, e => e.Action == AuditAction.PortalPdfDownloaded);
        Assert.Contains(events, e => e.Action == AuditAction.ShareLinkRevoked);
    }

    [Fact]
    public async Task CancelledInvoice_ReturnsNotFound_OnPortal()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = "Cancel Portal Client",
                ContactName = "Cancel Contact",
                Email = $"cancel-portal-{Guid.NewGuid():N}@billflow.test",
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
                        Description = "Cancel test",
                        Quantity = 1,
                        UnitPrice = 50m,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        await client.PostAsync($"/api/v1.0/billing/Invoice/Send/{invoice.Id}", null);

        var shareResponse = await client.PostAsync($"/api/v1.0/billing/Invoice/ShareLink/{invoice.Id}", null);
        var shareLink = await shareResponse.Content.ReadFromJsonAsync<ShareLinkResponse>(JsonOptions);
        Assert.NotNull(shareLink?.Token);

        await client.PostAsync($"/api/v1.0/billing/Invoice/Cancel/{invoice.Id}", null);

        var portalClient = fixture.ManagementFactory.CreateClient();
        var portalResponse = await portalClient.GetAsync($"/api/v1.0/portal/{shareLink.Token}");
        Assert.Equal(HttpStatusCode.NotFound, portalResponse.StatusCode);
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
                FullName = "Portal Visitor",
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
