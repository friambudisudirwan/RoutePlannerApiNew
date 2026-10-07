using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Models;
using RoutePlanner_Api.Services;

namespace RoutePlanner_Api.Controllers
{
    /// <summary>Generic planner endpoints for creating and integrating runsheets.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("Planner")]
    public class PlannerController
    (
        ILogger<PlannerController> logger,
        RunService runService,
        VRPConnectionFactory vrp
    ) : ControllerBase
    {
        private readonly ILogger<PlannerController> _logger = logger;
        private readonly RunService _runService = runService;

        /// <summary>Create runsheets from pool / car / trip planning data.</summary>
        /// <param name="param">Source, user context, and pool planning payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Runsheets created; returns list of RunID.</response>
        /// <response code="409">Business conflict while creating runsheets.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost("CreateRunsheets")]
        [EndpointSummary("Create runsheets")]
        [EndpointDescription("Creates runsheets from the provided pool, cars, and trips payload.")]
        [ProducesResponseType(typeof(CreateRunsheetsResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
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
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Internal server error." + ex.Message });
            }
        }

        /// <summary>Integrate existing runsheets into TMS EasyGO.</summary>
        /// <param name="param">List of runid / carid pairs to integrate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Integration succeeded; returns list of do_id.</response>
        /// <response code="422">Runsheet could not be processed.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost("IntegrateRunsheets")]
        [EndpointSummary("Integrate runsheets to TMS")]
        [EndpointDescription("Posts selected runsheets to TMS EasyGO and returns created delivery order IDs.")]
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
            catch (CustomException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(ex.status_code, ex.Message);
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

        [HttpGet("Result")]
        [EndpointSummary("Get route results")]
        [EndpointDescription("Get list planned and calculated trips result")]
        [ProducesResponseType(typeof(ResponseRouteResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetResults(string RunId, CancellationToken cancellationToken)
        {
            using var conn = vrp.CreateConnection();
            if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

            try
            {
                // get pool
                var pool = await _runService.GetPool(RunId, conn, null, cancellationToken);
                // get cars
                var cars = await _runService.GetCars(RunId, conn, null, cancellationToken);
                // get trips
                var trips = await _runService.GetTrips(RunId, conn, null, cancellationToken);
                // get trxroutes
                var routed = await _runService.GetTrxRoutes(RunId, conn, null, cancellationToken);
                // get trxroutedetails
                var route_details = await _runService.GetTrxRouteDetails(RunId, conn, null, cancellationToken);

                pool.Cars = cars;
                pool.Trips = trips;

                return StatusCode(StatusCodes.Status200OK, new ResponseRouteResult
                {
                    Pool = pool,
                    Routes = [.. routed.Select(x => x with
                    {
                        RouteDetails = [.. route_details.Where(y => y.RouteNo == x.RouteNo).OrderBy(y => y.Seq)]
                    })]
                });
            }
            catch (CustomException ex)
            {
                return StatusCode(ex.status_code, new MessageResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new MessageResponse { Message = ex.Message });
            }
        }
    }
}
