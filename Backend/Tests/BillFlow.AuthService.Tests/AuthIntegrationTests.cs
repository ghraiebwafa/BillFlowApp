using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using Xunit;

namespace BillFlow.AuthService.Tests;

public sealed class AuthIntegrationTests(AuthApiFixture fixture) : IClassFixture<AuthApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_Login_Profile_WorksEndToEnd()
    {
        var client = fixture.Factory.CreateClient();
        var email = $"visitor-{Guid.NewGuid():N}@billflow.test";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/register",
            new RegisterRequest
            {
                FullName = "Integration Visitor",
                Email = email,
                Password = "SecurePass123!",
                ConfirmPassword = "SecurePass123!",
            });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest { Email = email, Password = "SecurePass123!" });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(email, auth.User.Email);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var profileResponse = await client.GetAsync("/api/v1.0/auth/account/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndInvalidatesOldToken()
    {
        var client = fixture.Factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@billflow.test";

        await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/register",
            new RegisterRequest
            {
                FullName = "Refresh Test User",
                Email = email,
                Password = "SecurePass123!",
                ConfirmPassword = "SecurePass123!",
            });

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest { Email = email, Password = "SecurePass123!" });

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);

        var oldRefresh = auth.RefreshToken;

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = oldRefresh });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(rotated);
        Assert.NotEqual(oldRefresh, rotated.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync(
            "/api/v1.0/auth/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = oldRefresh });

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }
}
