using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Models;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlannerController
    (
        ILogger<PlannerController> logger,
        RunService runService
    ) : ControllerBase
    {
        private readonly ILogger<PlannerController> _logger = logger;
        private readonly RunService _runService = runService;


        [HttpPost("CreateRunsheets")]
        public async Task<IActionResult> CreateRunsheets(ParamCreateRunsheets param, CancellationToken cancellationToken)
        {
            try
            {
                var list_runid = await _runService.CreateRunsheets(param, cancellationToken);

                return StatusCode((int)HttpStatusCode.Created, new
                {
                    message = "Success",
                    data = list_runid.Select(x => new { RunID = x })
                });
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
