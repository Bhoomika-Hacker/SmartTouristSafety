using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/risk")]
    public class RiskApiController : ControllerBase
    {
        private readonly Services.IDynamicRiskZoneService _dynamicRiskZoneService;
        private readonly Services.IGeoFencingService _geoFencingService;
        private readonly Services.IExternalReportIngestionService _ingestionService;
        private readonly Services.IReverseGeocodingService _reverseGeocodingService;

        public RiskApiController(
            Services.IDynamicRiskZoneService dynamicRiskZoneService,
            Services.IGeoFencingService geoFencingService,
            Services.IExternalReportIngestionService ingestionService,
            Services.IReverseGeocodingService reverseGeocodingService)
        {
            _dynamicRiskZoneService = dynamicRiskZoneService;
            _geoFencingService = geoFencingService;
            _ingestionService = ingestionService;
            _reverseGeocodingService = reverseGeocodingService;
        }

        /// <summary>
        /// Live risk for any coordinate, computed automatically from real nearby reports —
        /// no zone has to have been drawn there first. If the point also happens to sit
        /// inside an administrator-declared zone, that's included too, but it's optional.
        /// </summary>
        [HttpGet("at")]
        public async Task<ActionResult<RiskAtDto>> GetRiskAt([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double? radiusMeters = null)
        {
            var dynamicRisk = _dynamicRiskZoneService.ComputeRiskAt(lat, lng, radiusMeters);
            var (zone, _) = _geoFencingService.FindContainingZone(lat, lng);
            var locationName = await _reverseGeocodingService.GetPlaceNameAsync(lat, lng);

            return Ok(new RiskAtDto
            {
                Latitude = lat,
                Longitude = lng,
                LocationName = locationName,
                RiskLevel = dynamicRisk.Level,
                Score = dynamicRisk.Score,
                ContributingReportCount = dynamicRisk.ContributingReportCount,
                RadiusMeters = dynamicRisk.RadiusMeters,
                NearestReportDistanceMeters = dynamicRisk.NearestReportDistanceMeters,
                IsInsideDeclaredZone = zone is not null,
                DeclaredZoneName = zone?.Name,
                DeclaredZoneRiskLevel = zone?.RiskLevel
            });
        }

        /// <summary>
        /// Manually kicks off an external report sync right now instead of waiting for the
        /// next background cycle — handy for demos/testing and for admins who want fresh
        /// data immediately after wiring in a new source.
        /// </summary>
        [HttpPost("sync-reports")]
        [Authorize(Roles = "Admin,Authority")]
        public async Task<IActionResult> SyncReports(CancellationToken ct)
        {
            var added = await _ingestionService.SyncAsync(ct);
            return Ok(new { reportsAdded = added });
        }
    }
}
