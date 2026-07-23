using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Models;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    /// <summary>Authentication endpoints for obtaining a JWT.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Auth")]
    public class AuthController
    (
        ILogger<AuthController> logger,
        AuthService authService
    ) : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly AuthService _authService = authService;

        /// <summary>Login and receive a JWT bearer token.</summary>
        /// <remarks>
        /// Use the returned <c>token</c> as <c>Authorization: Bearer {token}</c> on protected endpoints.
        /// </remarks>
        /// <param name="param">Credentials (<c>user_id</c>, <c>password</c>).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Login succeeded; JWT returned.</response>
        /// <response code="401">Invalid credentials.</response>
        /// <response code="500">Token generation or unexpected server error.</response>
        [HttpPost("Login")]
        [EndpointSummary("Login")]
        [EndpointDescription("Authenticates a user and returns a JWT for subsequent API calls.")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
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
    }
}
