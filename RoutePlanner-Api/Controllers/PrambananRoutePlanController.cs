using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PrambananRoutePlanController
    (
        ILogger<PrambananRoutePlanController> logger,
        RunService runService
    ) : ControllerBase
    {
        private readonly ILogger<PrambananRoutePlanController> _logger = logger;
        private readonly RunService _runService = runService;

        [HttpPost("CreateRunsheets")]
        public async Task<IActionResult> CreateRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
        {
            try
            {
                var list_runid = await _runService.CreatePrambananRunsheets(param, cancellationToken);

                return Ok(new
                {
                    message = "Success",
                    data = list_runid.Select(x => new { RunID = x })
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Unexpected error while creating prambanan runsheets");
                return Ok();
            }
            catch (CreateRunsheetException ex)
            {
                _logger.LogError(ex, "Failed when creating prambanan runsheets. Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed when creating runsheets. Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
        }
    }
}
