using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class ItemBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task BusinessOwner_CanManageItems_EndToEnd()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/items",
            new CreateItemRequest
            {
                Name = "Web Development",
                Description = "Hourly development work",
                UnitPrice = 85m,
                Currency = "usd",
                VatRate = 20m,
                Category = "Services",
                Unit = "hour",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Web Development", created.Name);
        Assert.Equal("USD", created.Currency);
        Assert.Equal(20m, created.VatRate);
        Assert.Equal("Services", created.Category);
        Assert.Equal("hour", created.Unit);

        var listResponse = await client.GetAsync("/api/v1.0/billing/items?search=web");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var items = await listResponse.Content.ReadFromJsonAsync<List<ItemResponse>>(JsonOptions);
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1.0/billing/items/{created.Id}",
            new UpdateItemRequest
            {
                Name = "Web Development Pro",
                Description = created.Description,
                UnitPrice = 95m,
                Currency = "USD",
                VatRate = 20m,
                Category = "Services",
                Unit = "hour",
                IsActive = true,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var archiveResponse = await client.PostAsync(
            $"/api/v1.0/billing/items/{created.Id}/archive",
            null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        var archivedListResponse = await client.GetAsync("/api/v1.0/billing/items");
        var activeItems = await archivedListResponse.Content.ReadFromJsonAsync<List<ItemResponse>>(JsonOptions);
        Assert.NotNull(activeItems);
        Assert.DoesNotContain(activeItems, i => i.Id == created.Id);

        var includeArchivedResponse = await client.GetAsync("/api/v1.0/billing/items?includeArchived=true");
        var allItems = await includeArchivedResponse.Content.ReadFromJsonAsync<List<ItemResponse>>(JsonOptions);
        Assert.NotNull(allItems);
        Assert.Contains(allItems, i => i.Id == created.Id && i.IsArchived);

        var deleteResponse = await client.DeleteAsync($"/api/v1.0/billing/items/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_ReturnsBadRequest_WhenArchived()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/items",
            new CreateItemRequest
            {
                Name = "Archived Service",
                UnitPrice = 50m,
            });
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>(JsonOptions);
        Assert.NotNull(created);

        await client.PostAsync($"/api/v1.0/billing/items/{created.Id}/archive", null);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1.0/billing/items/{created.Id}",
            new UpdateItemRequest
            {
                Name = "Should Fail",
                UnitPrice = 60m,
                Currency = "USD",
            });
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
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
                FullName = "Item Test Visitor",
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
