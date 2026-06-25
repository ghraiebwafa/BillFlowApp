using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Repositories.Security;
using BillFlow.Shared.Caching;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Security;

namespace BillFlow.AuthService.Services;

public sealed class AccountService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ICacheService cache,
    IUserSessionRevocationService sessionRevocation,
    ICurrentUserAccessor currentUser,
    IHostEnvironment environment,
    JwtOptions jwtOptions) : IAccountService
{
    public async Task<AccountResult<MessageResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return AccountResult<MessageResponse>.Ok(
                new MessageResponse { Message = AuthConstants.RegistrationSuccessMessage },
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
            IsEmailConfirmed = environment.IsDevelopment(),
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await userRepository.CreateAsync(user, cancellationToken);

        return AccountResult<MessageResponse>.Ok(
            new MessageResponse { Message = AuthConstants.RegistrationSuccessMessage },
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

        if (!environment.IsDevelopment() && !user.IsEmailConfirmed)
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

        if (await cache.ExistsAsync(CacheKeys.RevokedRefreshToken(tokenHash), cancellationToken))
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

        await cache.SetAsync(
            CacheKeys.RevokedRefreshToken(rotation.OldTokenHash),
            true,
            TimeSpan.FromDays(jwtOptions.RefreshTokenDays),
            cancellationToken);

        var response = await BuildAuthResponseAsync(rotation.User, newRefreshPlain, cancellationToken);
        return AccountResult<AuthResponse>.Ok(response);
    }

    public async Task<AccountResult<MessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!DevFeatureFlags.IsDevResetPasswordEnabled(environment))
        {
            return AccountResult<MessageResponse>.Fail(
                "Not found.",
                StatusCodes.Status404NotFound);
        }

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

            await cache.SetAsync(
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
