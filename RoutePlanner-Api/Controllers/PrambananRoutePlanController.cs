using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    /// <summary>Prambanan-specific route planning, PS updates, and TMS integration.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("PrambananRoutePlan")]
    public class PrambananRoutePlanController
    (
        ILogger<PrambananRoutePlanController> logger,
        PrambananRunService runService,
        ActionLogService logService
    ) : ControllerBase
    {
        private readonly ILogger<PrambananRoutePlanController> _logger = logger;
        private readonly PrambananRunService _runService = runService;
        private readonly ActionLogService _logService = logService;

        /// <summary>Create Prambanan runsheets (manual or automatic routing).</summary>
        /// <remarks>
        /// Routing mode is chosen from the payload:
        /// - If any trip has <c>car_plate</c> filled → <b>manual routing</b>.
        /// - Otherwise → <b>automatic planning</b>.
        /// </remarks>
        /// <param name="param">Start time and trip list for Prambanan planning.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Runsheets created; returns list of RunID.</response>
        /// <response code="400">Validation failed (duplicate SO and/or invalid lon/lat).</response>
        /// <response code="409">Business conflict while creating runsheets.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost("CreateRunsheets")]
        [EndpointSummary("Create Prambanan runsheets")]
        [EndpointDescription("Creates Prambanan runsheets using manual routing when car_plate is present, otherwise automatic planning.")]
        [ProducesResponseType(typeof(CreateRunsheetsResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(PrambananValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
        {
            try
            {
                var list_runid = new List<string>();
                var check_is_manual_routing = param.Data.Count(x => !string.IsNullOrWhiteSpace(x.PoliceNo));

                if (check_is_manual_routing > 0)
                {
                    // ** manual routing
                    var fetch_list_runid = await _runService.CreatePrambananManualRunsheets(param, cancellationToken);
                    list_runid.AddRange(fetch_list_runid);
                }
                else
                {
                    // ** automatic planning
                    var fetch_list_runid = await _runService.CreatePrambananRunsheets(param, cancellationToken);
                    list_runid.AddRange(list_runid);
                }

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

        /// <summary>Update PL/PS values for sales orders in GPSB.</summary>
        /// <param name="param">List of SO / PL / PS updates.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">PS data updated successfully.</response>
        /// <response code="404">One or more SO rows were not found in GPSB.</response>
        /// <response code="500">Unexpected server error (includes <c>trace_id</c>).</response>
        [HttpPost("UpdatePS")]
        [EndpointSummary("Update PS")]
        [EndpointDescription("Updates PL and PS fields for the given sales order numbers.")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UpdatePSNotFoundResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(UpdatePSErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePS(ParamUpdatePS param, CancellationToken cancellationToken)
        {
            var trace_id = Guid.NewGuid().ToString();

            try
            {
                await _logService.CreateLog
                (
                    runid: string.Empty,
                    type: "parameter",
                    action_name: "Controller.UpdatePS",
                    log_body: JsonConvert.SerializeObject(param),
                    trace_id: trace_id,
                    cancellationToken: cancellationToken
                );

                await _runService.UpdatePS(param, cancellationToken);

                await _logService.CreateLog
                (
                    runid: string.Empty,
                    type: "success",
                    action_name: "PrambananRunService.UpdatePS",
                    log_body: JsonConvert.SerializeObject(param.Data),
                    trace_id: trace_id,
                    cancellationToken: cancellationToken
                );

                return Ok(new { message = "PS data updated successfully" });
            }
            catch (UpdatePSNotFoundException ex)
            {
                return StatusCode((int)HttpStatusCode.NotFound, new { list_not_found_so = ex.ListNotFoundSo });
            }
            catch (Exception ex)
            {
                await _logService.CreateLog
                (
                    runid: string.Empty,
                    type: "error",
                    action_name: "PrambananRunService.UpdatePS",
                    log_body: JsonConvert.SerializeObject(new { message = ex.Message }),
                    trace_id: trace_id,
                    cancellationToken: cancellationToken
                );

                _logger.LogError(ex, "Failed when updating PS. Internal server error.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error.", trace_id });
            }
        }

        /// <summary>Integrate Prambanan runsheets into TMS EasyGO.</summary>
        /// <param name="param">List of runid / carid pairs to integrate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Integration succeeded; returns list of do_id.</response>
        /// <response code="422">Runsheet could not be processed.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost("IntegrateRunsheets")]
        [EndpointSummary("Integrate Prambanan runsheets to TMS")]
        [EndpointDescription("Posts selected Prambanan runsheets to TMS EasyGO and returns created delivery order IDs.")]
        [ProducesResponseType(typeof(IntegrateRunsheetsResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
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
                _logger.LogWarning(ex, "Unexpected error while creating prambanan runsheets at {time}", DateTime.Now);
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = $"Internal server error. {ex.Message}" });
            }
            catch (CreateRunsheetException ex)
            {
                _logger.LogWarning(ex, "Failed when integrating prambanan runsheets at {time}.", DateTime.Now);
                return StatusCode((int)HttpStatusCode.UnprocessableEntity, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed when creating runsheets. Internal server error. at {time}", DateTime.Now);
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." });
            }
        }

    }
}
