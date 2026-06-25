using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class ClientBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task BusinessOwner_CanManageClients_EndToEnd()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = "Acme Corp",
                ContactName = "Jane Doe",
                Email = $"client-{Guid.NewGuid():N}@billflow.test",
                PhoneNumber = "+1-555-0100",
                Address = "123 Main St",
                Country = "US",
                TaxNumber = "TAX-12345",
                Notes = "Preferred client",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Acme Corp", created.CompanyName);
        Assert.Equal("US", created.Country);
        Assert.Equal("TAX-12345", created.TaxNumber);

        var listResponse = await client.GetAsync("/api/v1.0/billing/Client/GetAll");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var clients = await listResponse.Content.ReadFromJsonAsync<List<ClientResponse>>(JsonOptions);
        Assert.NotNull(clients);
        Assert.Contains(clients, c => c.Id == created.Id);

        var getResponse = await client.GetAsync($"/api/v1.0/billing/Client/GetById/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1.0/billing/Client/Update/{created.Id}",
            new UpdateClientRequest
            {
                CompanyName = "Acme Corporation",
                ContactName = created.ContactName,
                Email = created.Email,
                Country = "CA",
                IsActive = true,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Acme Corporation", updated.CompanyName);
        Assert.Equal("CA", updated.Country);

        var deleteResponse = await client.DeleteAsync($"/api/v1.0/billing/Client/Delete/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateClient_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var email = $"dup-{Guid.NewGuid():N}@billflow.test";

        var request = new CreateClientRequest
        {
            CompanyName = "First Co",
            ContactName = "Alice",
            Email = email,
        };

        var first = await client.PostAsJsonAsync("/api/v1.0/billing/Client/Create", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        request.CompanyName = "Second Co";
        var second = await client.PostAsJsonAsync("/api/v1.0/billing/Client/Create", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_CannotAccessClientBilling()
    {
        var authClient = fixture.AuthFactory.CreateClient();
        var loginResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest
            {
                Email = fixture.SuperAdminEmailAddress,
                Password = fixture.SuperAdminPasswordValue,
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);

        var managementClient = fixture.ManagementFactory.CreateClient();
        managementClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await managementClient.GetAsync("/api/v1.0/billing/Client/GetAll");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> RegisterAndLoginVisitorAsync()
    {
        var authClient = fixture.AuthFactory.CreateClient();
        var email = $"visitor-{Guid.NewGuid():N}@billflow.test";
        const string password = "SecurePass123!";

        var registerResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/register",
            new RegisterRequest
            {
                FullName = "Billing Visitor",
                Email = email,
                Password = password,
                ConfirmPassword = password,
            });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);
        return auth.AccessToken;
    }

    private HttpClient CreateManagementClient(string accessToken)
    {
        var client = fixture.ManagementFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
