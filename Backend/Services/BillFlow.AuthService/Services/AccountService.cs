using System.Security.Cryptography;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.Security;
using BillFlow.Shared.Caching;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Email;
using BillFlow.Shared.Security;

namespace BillFlow.AuthService.Services;

public sealed class AccountService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthEmailTokenRepository authEmailTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ICacheService cache,
    IUserSessionRevocationService sessionRevocation,
    ICurrentUserAccessor currentUser,
    IHostEnvironment environment,
    IEmailSender emailSender,
    FrontendUrlOptions frontendUrls,
    JwtOptions jwtOptions) : IAccountService
{
    private static bool RequireEmailVerification() =>
        BillFlowEnv.GetBool("REQUIRE_EMAIL_VERIFICATION", defaultValue: false);

    public async Task<AccountResult<MessageResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var requireVerification = RequireEmailVerification();
        // Same message for existing vs new when verification is required (anti-enumeration).
        var acceptedMessage = requireVerification
            ? AuthConstants.RegistrationVerifyEmailMessage
            : AuthConstants.RegistrationSuccessMessage;

        if (await userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return AccountResult<MessageResponse>.Ok(
                new MessageResponse { Message = acceptedMessage },
                StatusCodes.Status200OK);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Visitor,
            IsActive = true,
            IsEmailConfirmed = !requireVerification,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await userRepository.CreateAsync(user, cancellationToken);

        if (requireVerification)
            await IssueAndEmailTokenAsync(user, AuthEmailTokenPurpose.EmailVerification, cancellationToken);

        return AccountResult<MessageResponse>.Ok(
            new MessageResponse { Message = acceptedMessage },
            StatusCodes.Status200OK);
    }

    public async Task<AccountResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(user, request.Password, user.PasswordHash))
        {
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.GenericAuthFailureMessage,
                StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.GenericAuthFailureMessage,
                StatusCodes.Status401Unauthorized);
        }

        if (RequireEmailVerification() && !user.IsEmailConfirmed)
        {
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.EmailNotVerifiedMessage,
                StatusCodes.Status403Forbidden);
        }

        await userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);
        user.LastLoginAt = DateTime.UtcNow;

        var response = await IssueAuthResponseAsync(user, cancellationToken);
        return AccountResult<AuthResponse>.Ok(response);
    }

    public async Task<AccountResult<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);

        if (await cache.ExistsSafeAsync(CacheKeys.RevokedRefreshToken(tokenHash), cancellationToken))
        {
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.GenericAuthFailureMessage,
                StatusCodes.Status401Unauthorized);
        }

        var anyState = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (anyState is { RevokedAt: not null })
        {
            await sessionRevocation.RevokeAllSessionsAsync(anyState.UserId, cancellationToken);
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.GenericAuthFailureMessage,
                StatusCodes.Status401Unauthorized);
        }

        var newRefreshPlain = jwtTokenService.GenerateRefreshToken();
        var newRefreshHash = TokenHasher.Hash(newRefreshPlain);

        var rotation = await refreshTokenRepository.RotateActiveTokenAsync(
            tokenHash,
            new RefreshToken
            {
                Token = newRefreshHash,
                ExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            },
            cancellationToken);

        if (rotation is null)
        {
            return AccountResult<AuthResponse>.Fail(
                AuthConstants.GenericAuthFailureMessage,
                StatusCodes.Status401Unauthorized);
        }

        await cache.SetSafeAsync(
            CacheKeys.RevokedRefreshToken(rotation.OldTokenHash),
            true,
            TimeSpan.FromDays(jwtOptions.RefreshTokenDays),
            cancellationToken);

        var response = await BuildAuthResponseAsync(rotation.User, newRefreshPlain, cancellationToken);
        return AccountResult<AuthResponse>.Ok(response);
    }

    public async Task<AccountResult<MessageResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var generic = new MessageResponse { Message = AuthConstants.ForgotPasswordAcceptedMessage };
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is { IsActive: true })
            await IssueAndEmailTokenAsync(user, AuthEmailTokenPurpose.PasswordReset, cancellationToken);

        return AccountResult<MessageResponse>.Ok(generic);
    }

    public async Task<AccountResult<MessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            var hash = TokenHasher.Hash(request.Token.Trim());
            var stored = await authEmailTokenRepository.GetActiveByHashAsync(
                hash,
                AuthEmailTokenPurpose.PasswordReset,
                cancellationToken);

            if (stored?.User is null || !stored.User.IsActive)
            {
                return AccountResult<MessageResponse>.Fail(
                    AuthConstants.GenericResetFailureMessage,
                    StatusCodes.Status400BadRequest);
            }

            stored.User.PasswordHash = passwordHasher.HashPassword(stored.User, request.NewPassword);
            await userRepository.UpdateAsync(stored.User, cancellationToken);
            await authEmailTokenRepository.MarkUsedAsync(stored.Id, cancellationToken);
            await sessionRevocation.RevokeAllSessionsAsync(stored.User.Id, cancellationToken);

            return AccountResult<MessageResponse>.Ok(new MessageResponse
            {
                Message = "Password has been reset successfully.",
            });
        }

        if (DevFeatureFlags.IsDevResetPasswordEnabled(environment)
            && !string.IsNullOrWhiteSpace(request.Email))
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return AccountResult<MessageResponse>.Fail(
                    AuthConstants.GenericResetFailureMessage,
                    StatusCodes.Status400BadRequest);
            }

            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
            await userRepository.UpdateAsync(user, cancellationToken);
            await sessionRevocation.RevokeAllSessionsAsync(user.Id, cancellationToken);

            return AccountResult<MessageResponse>.Ok(new MessageResponse
            {
                Message = "Password has been reset successfully.",
            });
        }

        return AccountResult<MessageResponse>.Fail(
            AuthConstants.GenericResetFailureMessage,
            StatusCodes.Status400BadRequest);
    }

    public async Task<AccountResult<MessageResponse>> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = TokenHasher.Hash(request.Token.Trim());
        var stored = await authEmailTokenRepository.GetActiveByHashAsync(
            hash,
            AuthEmailTokenPurpose.EmailVerification,
            cancellationToken);

        if (stored?.User is null)
        {
            return AccountResult<MessageResponse>.Fail(
                AuthConstants.GenericOtpFailureMessage,
                StatusCodes.Status400BadRequest);
        }

        await userRepository.ConfirmEmailAsync(stored.UserId, cancellationToken);
        await authEmailTokenRepository.MarkUsedAsync(stored.Id, cancellationToken);

        return AccountResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Email verified successfully. You can sign in now.",
        });
    }

    public async Task<AccountResult<MessageResponse>> ResendVerificationAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var generic = new MessageResponse { Message = AuthConstants.VerificationEmailSentMessage };
        if (!RequireEmailVerification())
            return AccountResult<MessageResponse>.Ok(generic);

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is { IsActive: true, IsEmailConfirmed: false })
            await IssueAndEmailTokenAsync(user, AuthEmailTokenPurpose.EmailVerification, cancellationToken);

        return AccountResult<MessageResponse>.Ok(generic);
    }

    public async Task<AccountResult<UserProfileResponse>> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return AccountResult<UserProfileResponse>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized);
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return AccountResult<UserProfileResponse>.Fail(
                "Account not found.",
                StatusCodes.Status404NotFound);
        }

        return AccountResult<UserProfileResponse>.Ok(MapToProfile(user));
    }

    public async Task<AccountResult<MessageResponse>> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return AccountResult<MessageResponse>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized);
        }

        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var stored = await refreshTokenRepository.GetActiveByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is not null && stored.UserId == userId.Value)
        {
            await refreshTokenRepository.RevokeAsync(stored.Id, cancellationToken: cancellationToken);

            await cache.SetSafeAsync(
                CacheKeys.RevokedRefreshToken(tokenHash),
                true,
                TimeSpan.FromDays(jwtOptions.RefreshTokenDays),
                cancellationToken);
        }

        return AccountResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Logged out successfully.",
        });
    }

    public async Task<AccountResult<MessageResponse>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return AccountResult<MessageResponse>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized);
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return AccountResult<MessageResponse>.Fail(
                "Account not found.",
                StatusCodes.Status404NotFound);
        }

        if (!passwordHasher.VerifyPassword(user, request.CurrentPassword, user.PasswordHash))
        {
            return AccountResult<MessageResponse>.Fail(
                "Current password is incorrect.",
                StatusCodes.Status400BadRequest);
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await userRepository.UpdateAsync(user, cancellationToken);
        await sessionRevocation.RevokeAllSessionsAsync(user.Id, cancellationToken);

        return AccountResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Password changed successfully. Please sign in again.",
        });
    }

    public async Task<AccountResult<MessageResponse>> DeactivateAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return AccountResult<MessageResponse>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized);
        }

        await userRepository.SoftDeleteAsync(userId.Value, cancellationToken);
        await sessionRevocation.RevokeAllSessionsAsync(userId.Value, cancellationToken);

        return AccountResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Account deactivated successfully.",
        });
    }

    public async Task<AccountResult<MessageResponse>> DeleteAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return AccountResult<MessageResponse>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized);
        }

        await sessionRevocation.RevokeAllSessionsAsync(userId.Value, cancellationToken);

        if (await userRepository.HasBillingDataAsync(userId.Value, cancellationToken))
        {
            return AccountResult<MessageResponse>.Fail(
                "This account has billing data. Deactivate the account instead of permanently deleting it.",
                StatusCodes.Status409Conflict);
        }

        await userRepository.HardDeleteAsync(userId.Value, cancellationToken);

        return AccountResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Account permanently deleted.",
        });
    }

    private async Task IssueAndEmailTokenAsync(
        User user,
        AuthEmailTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        await authEmailTokenRepository.InvalidateActiveAsync(user.Id, purpose, cancellationToken);

        var plain = CreateSecureToken();
        var lifetime = purpose == AuthEmailTokenPurpose.PasswordReset
            ? TimeSpan.FromHours(1)
            : TimeSpan.FromHours(24);

        await authEmailTokenRepository.CreateAsync(
            new AuthEmailToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = TokenHasher.Hash(plain),
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
            },
            cancellationToken);

        var url = purpose == AuthEmailTokenPurpose.PasswordReset
            ? frontendUrls.ResetPasswordUrl(plain)
            : frontendUrls.VerifyEmailUrl(plain);

        var message = purpose == AuthEmailTokenPurpose.PasswordReset
            ? AuthEmailComposer.PasswordReset(user.Email, user.FullName, url)
            : AuthEmailComposer.EmailVerification(user.Email, user.FullName, url);

        await emailSender.SendAsync(message, cancellationToken);
    }

    private static string CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var refreshPlain = jwtTokenService.GenerateRefreshToken();

        await refreshTokenRepository.CreateAsync(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = TokenHasher.Hash(refreshPlain),
                ExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            },
            cancellationToken);

        return await BuildAuthResponseAsync(user, refreshPlain, cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        User user,
        string refreshTokenPlain,
        CancellationToken cancellationToken) => new()
    {
        AccessToken = await jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken),
        RefreshToken = refreshTokenPlain,
        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes),
        User = MapToProfile(user),
    };

    private static UserProfileResponse MapToProfile(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        IsEmailConfirmed = user.IsEmailConfirmed,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
    };
}
