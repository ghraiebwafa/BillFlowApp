using BillFlow.Models.Dtos.Auth.Account;

namespace BillFlow.AuthService.Services;

public interface IAccountService
{
    Task<AccountResult<MessageResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<UserProfileResponse>> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<AccountResult<MessageResponse>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<MessageResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<AccountResult<MessageResponse>> DeactivateAsync(CancellationToken cancellationToken = default);

    Task<AccountResult<MessageResponse>> DeleteAsync(CancellationToken cancellationToken = default);
}
