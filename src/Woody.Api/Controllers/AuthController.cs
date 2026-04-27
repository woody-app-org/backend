using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IAuthSessionService _authSessions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LoginHandler loginHandler,
        RegisterHandler registerHandler,
        IEmailVerificationService emailVerificationService,
        IAuthSessionService authSessions,
        ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
        _emailVerificationService = emailVerificationService;
        _authSessions = authSessions;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _loginHandler.HandleAsync(request, cancellationToken);
            _logger.LogInformation("User logged in successfully.");
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = LoginHandler.InvalidCredentialsMessage });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registerHandler.HandleAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerificationCode(
        [FromBody] SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _emailVerificationService.SendCodeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerificationCode(
        [FromBody] SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _emailVerificationService.ResendCodeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailCode(
        [FromBody] ConfirmEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _emailVerificationService.ConfirmCodeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authSessions.RefreshAsync(request.RefreshToken, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = LoginHandler.InvalidCredentialsMessage });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequestDTO? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _authSessions.RevokeRefreshTokenAsync(
            request?.RefreshToken,
            userId,
            "logout",
            cancellationToken);
        return NoContent();
    }
}
