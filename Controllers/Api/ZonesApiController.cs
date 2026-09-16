using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/zones")]
    public class ZonesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ZonesApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult<IEnumerable<GeoFenceZone>> GetAll()
        {
            return Ok(_db.GeoFenceZones.Where(z => z.IsActive).ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<GeoFenceZone> GetById(int id)
        {
            var zone = _db.GeoFenceZones.Find(id);
            return zone is null ? NotFound() : Ok(zone);
        }

        [HttpPost]
        public ActionResult<GeoFenceZone> Create(GeoFenceZone zone)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.GeoFenceZones.Add(zone);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = zone.Id }, zone);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, GeoFenceZone model)
        {
            var zone = _db.GeoFenceZones.Find(id);
            if (zone is null) return NotFound();

            zone.Name = model.Name;
            zone.Description = model.Description;
            zone.Latitude = model.Latitude;
            zone.Longitude = model.Longitude;
            zone.RadiusMeters = model.RadiusMeters;
            zone.RiskLevel = model.RiskLevel;
            zone.IsActive = model.IsActive;

            _db.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var zone = _db.GeoFenceZones.Find(id);
            if (zone is null) return NotFound();
            _db.GeoFenceZones.Remove(zone);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
