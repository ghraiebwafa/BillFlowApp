using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Management;
using BillFlow.Models.Shared.Enums;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class ManagementIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = fixture.ManagementFactory.CreateClient();
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuperAdmin_CanCreateAdmin()
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
        Assert.Equal(UserRole.SuperAdmin, auth.User.Role);

        var managementClient = fixture.ManagementFactory.CreateClient();
        managementClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var adminEmail = $"admin-{Guid.NewGuid():N}@billflow.test";
        var createResponse = await managementClient.PostAsJsonAsync(
            "/api/v1.0/management/Admin/Create",
            new CreateAdminRequest
            {
                FullName = "Integration Admin",
                Email = adminEmail,
                Password = "AdminPass123!",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<UserManagementResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(adminEmail, created.Email);
        Assert.Equal(UserRole.Admin, created.Role);
    }
}
