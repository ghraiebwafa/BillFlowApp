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
public sealed class AuditTrailIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ClientCreate_RecordsActivityEvent()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var companyName = $"Audit Client {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/Client/Create",
            new CreateClientRequest
            {
                CompanyName = companyName,
                ContactName = "Audit Tester",
                Email = $"audit-{Guid.NewGuid():N}@billflow.test",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var activityResponse = await client.GetAsync("/api/v1.0/billing/Activity/GetRecent?limit=10");
        Assert.Equal(HttpStatusCode.OK, activityResponse.StatusCode);

        var events = await activityResponse.Content.ReadFromJsonAsync<List<AuditEventResponse>>(JsonOptions);
        Assert.NotNull(events);
        Assert.Contains(
            events,
            e => e.EntityType == AuditEntityType.Client
                && e.Action == AuditAction.Created
                && e.Summary.Contains(companyName, StringComparison.Ordinal));
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
                FullName = "Audit Visitor",
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
