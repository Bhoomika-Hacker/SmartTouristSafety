using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/location")]
    public class LocationApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IIncidentResponseService _incidentResponseService;

        public LocationApiController(ApplicationDbContext db, IIncidentResponseService incidentResponseService)
        {
            _db = db;
            _incidentResponseService = incidentResponseService;
        }

        /// <summary>
        /// Core ingestion endpoint for the tourist mobile app / wearable tracker /
        /// the browser geolocation call from the tourist portal dashboard. Every
        /// GPS ping is run through geo-fencing + the AI risk model, the nearest
        /// police station is resolved, and the pipeline auto-raises alerts/
        /// incidents when required. Left open to anonymous IoT trackers, but a
        /// signed-in Tourist account may only ping on behalf of their own record.
        /// </summary>
        [HttpPost("ping")]
        public async Task<ActionResult<LocationPingResultDto>> Ping(LocationPingDto dto)
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Tourist"))
            {
                var claim = User.FindFirst("TouristId");
                if (claim is null || !int.TryParse(claim.Value, out var ownTouristId) || ownTouristId != dto.TouristId)
                {
                    return Forbid();
                }
            }

            try
            {
                var result = await _incidentResponseService.ProcessLocationPingAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // GET api/location/history/5
        [HttpGet("history/{touristId:int}")]
        public IActionResult History(int touristId, [FromQuery] int take = 25)
        {
            var logs = _db.LocationLogs
                .Include(l => l.Zone)
                .Where(l => l.TouristId == touristId)
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToList();

            return Ok(logs);
        }
    }
}
