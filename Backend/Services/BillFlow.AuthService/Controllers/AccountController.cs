using BillFlow.AuthService.Services;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.AuthService.Controllers;

[ApiController]
[Route("api/v1.0/auth/account")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.RegisterAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.LoginAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.RefreshTokenAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.ForgotPasswordAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.ResetPasswordAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.ConfirmEmailAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ResendVerification(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.ResendVerificationAsync(request, cancellationToken));

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Profile(CancellationToken cancellationToken) =>
        ToActionResult(accountService.GetProfileAsync(cancellationToken));

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.LogoutAsync(request, cancellationToken));

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(accountService.ChangePasswordAsync(request, cancellationToken));

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpDelete("deactivate")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Deactivate(CancellationToken cancellationToken) =>
        ToActionResult(accountService.DeactivateAsync(cancellationToken));

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(CancellationToken cancellationToken) =>
        ToActionResult(accountService.DeleteAsync(cancellationToken));

    private async Task<IActionResult> ToActionResult<T>(Task<AccountResult<T>> task)
    {
        var result = await task;

        if (result.IsSuccess)
            return StatusCode(result.StatusCode, result.Value);

        return Problem(
            detail: result.Error,
            statusCode: result.StatusCode,
            title: result.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status429TooManyRequests => "Too Many Requests",
                _ => "Bad Request",
            });
    }
}
