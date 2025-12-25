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
        PrambananRunService runService
    ) : ControllerBase
    {
        private readonly ILogger<PrambananRoutePlanController> _logger = logger;
        private readonly PrambananRunService _runService = runService;

        [HttpPost("CreateRunsheets")]
        public async Task<IActionResult> CreateRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
        {
            try
            {
                var list_runid = await _runService.CreatePrambananRunsheets(param, cancellationToken);

                return StatusCode((int)HttpStatusCode.Created, new
                {
                    message = "Success",
                    data = list_runid.Select(x => new { RunID = x })
                });
            }
            catch (PrambananSoValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed");
                return StatusCode((int)HttpStatusCode.BadRequest, new { message = ex.Message, duplicate_so = ex.ListDuplicateSo, not_valid_lon_lat = ex.ListNotValidLonLat });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Unexpected error while creating prambanan runsheets");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
            catch (CreateRunsheetException ex)
            {
                _logger.LogError(ex, "Failed when creating prambanan runsheaaets.");
                return StatusCode((int)HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed when creating runsheets. Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
        }

        [HttpPost("IntegrateRunsheets")]
        public async Task<IActionResult> IntegrateRunsheets(ParamIntegrateRunsheets param, CancellationToken cancellationToken)
        {
            try
            {
                // ** hit post do
                var list_do_id = await _runService.IntegrateRunsheets(param, cancellationToken);

                return StatusCode((int)HttpStatusCode.Created, new
                {
                    message = "Runsheets berhasil diintegrasikan ke TMS EasyGO.",
                    data = list_do_id.Select(x => new
                    {
                        do_id = x
                    })
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Unexpected error while creating prambanan runsheets");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = $"Internal server error. {ex.Message}" });
            }
            catch (CreateRunsheetException ex)
            {
                _logger.LogWarning(ex, "Failed when integrating prambanan runsheets.");
                return StatusCode((int)HttpStatusCode.UnprocessableEntity, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed when creating runsheets. Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
        }

    }
}
