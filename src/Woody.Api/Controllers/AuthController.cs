using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Application.DTOs;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LoginHandler loginHandler,
        RegisterHandler registerHandler,
        ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
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

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}
