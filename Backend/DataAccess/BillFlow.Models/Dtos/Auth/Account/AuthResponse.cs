namespace BillFlow.Models.Dtos.Auth.Account;

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime AccessTokenExpiresAt { get; set; }

    public UserProfileResponse User { get; set; } = null!;

    public string? Message { get; set; }
}