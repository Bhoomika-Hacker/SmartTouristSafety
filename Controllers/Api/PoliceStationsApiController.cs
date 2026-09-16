using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/police-stations")]
    public class PoliceStationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPoliceStationService _policeStationService;

        public PoliceStationsApiController(ApplicationDbContext db, IPoliceStationService policeStationService)
        {
            _db = db;
            _policeStationService = policeStationService;
        }

        // GET api/police-stations
        [HttpGet]
        public ActionResult<IEnumerable<PoliceStation>> GetAll()
        {
            return Ok(_db.PoliceStations.Where(p => p.IsActive).ToList());
        }

        // GET api/police-stations/nearest?lat=..&lng=..  (used by the tourist portal — live worldwide lookup)
        [HttpGet("nearest")]
        public async Task<IActionResult> Nearest([FromQuery] double lat, [FromQuery] double lng)
        {
            var nearest = await _policeStationService.FindNearestAsync(lat, lng);
            return nearest is null ? NotFound("No police station could be found near that location.") : Ok(nearest);
        }

        // POST api/police-stations  (admin/authority only)
        [HttpPost]
        [Authorize(Roles = "Admin,Authority,Operator")]
        public ActionResult<PoliceStation> Create(PoliceStation station)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.PoliceStations.Add(station);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetAll), station);
        }
    }
}
