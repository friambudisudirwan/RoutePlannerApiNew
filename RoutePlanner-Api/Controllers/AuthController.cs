using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoutePlanner_Api.Models;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController
    (
        ILogger<AuthController> logger,
        AuthService authService
    ) : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly AuthService _authService = authService;

        [HttpPost("Login")]
        public async Task<IActionResult> Login(ConfMstUser param, CancellationToken cancellationToken)
        {
            try
            {
                var authenticate = await _authService.LoginAsync(param.UserID, param.Password, cancellationToken);
                if (!authenticate.result) return StatusCode((int)HttpStatusCode.Unauthorized, new { message = authenticate.message });

                if (string.IsNullOrEmpty(authenticate.token)) throw new InvalidOperationException("Failed when generating token. Internal server error");

                return Ok(new { message = "Login success.", authenticate.token });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Internal server error while getting generated token value");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "Internal server error.");
            }
        }

        // [Authorize]
        // [HttpGet("TestAuthenticate")]
        // public IActionResult TestAuthenticate()
        // {
        //     var a = "test";
        //     return Ok(User.FindFirst("UserId").Value);
        // }
    }
}
