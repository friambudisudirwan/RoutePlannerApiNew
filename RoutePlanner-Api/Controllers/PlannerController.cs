using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Models;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlannerController
    (
        ILogger<PlannerController> logger,
        IConfiguration config,
        AuthService authService,
        RunService runService
    ) : ControllerBase
    {
        private readonly ILogger<PlannerController> _logger = logger;
        private readonly AuthService _authService = authService;
        private readonly RunService _runService = runService;
        private readonly string _connectionstringVRP = config.GetConnectionString("VRP") ?? throw new ArgumentNullException("Connection string VRP is empty");
        private readonly string _connectionstringGPSB = config.GetConnectionString("GPSB") ?? throw new ArgumentNullException("Connection string GPSB is empty");


        [HttpPost("CreateRunsheets")]
        public async Task<IActionResult> CreateRunsheets(ParamCreateRunsheets param, CancellationToken cancellationToken)
        {
            try
            {
                // ** authenticate user
                var authenticate = await _authService.AuthenticateAsync(param.User.UserID, param.User.Password, cancellationToken);
                if (!authenticate.result) return StatusCode((int)HttpStatusCode.Unauthorized, new { authenticate.message });

                // ** begin create runsheets
                var runids = await _runService.CreateRunsheets(param, cancellationToken);
                // if (runids.Count < 1) return StatusCode((int)HttpStatusCode.Conflict, new { message = "No runsheets created." });

                return Ok(param);
            }
            catch (CreateRunsheetException ex)
            {
                _logger.LogWarning(ex, "Failed when creating runsheet.");
                return StatusCode((int)HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed when creating runsheet.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
        }
    }
}
