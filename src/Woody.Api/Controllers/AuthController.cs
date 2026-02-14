using Microsoft.AspNetCore.Mvc;
using Woody.Application.DTOs.Login;
using Woody.Application.UseCases.Auth.Login;

namespace Woody.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginHandler _handler;
        private readonly ILogger<AuthController> _logger;
        public AuthController(LoginHandler handler, ILogger<AuthController> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            var result = await _handler.HandleAsync(request);
            _logger.LogInformation("User logged in successfully.");
            return Ok(result);
        }
    }
}