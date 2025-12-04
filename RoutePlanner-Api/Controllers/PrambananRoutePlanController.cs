using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PrambananRoutePlanController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateRunsheets(ApiMstTrip param, CancellationToken cancellationToken)
        {
            
        }
    }
}
