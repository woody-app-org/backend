using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LoginHandler loginHandler,
        RegisterHandler registerHandler,
        IEmailVerificationService emailVerificationService,
        ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        var result = await _loginHandler.HandleAsync(request);
        _logger.LogInformation("User logged in successfully.");
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(request, cancellationToken);
        return Ok(result);
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

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}
