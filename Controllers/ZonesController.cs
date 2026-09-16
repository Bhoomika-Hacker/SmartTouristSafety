using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Controllers
{
    [Authorize(Roles = "Admin,Authority,Operator")]
    public class ZonesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ZonesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var zones = _db.GeoFenceZones.OrderBy(z => z.Name).ToList();
            return View(zones);
        }

        public IActionResult Details(int id)
        {
            var zone = _db.GeoFenceZones.Find(id);
            if (zone is null) return NotFound();
            return View(zone);
        }

        [HttpGet]
        public IActionResult Create() => View(new GeoFenceZone());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GeoFenceZone zone)
        {
            if (!ModelState.IsValid) return View(zone);
            _db.GeoFenceZones.Add(zone);
            _db.SaveChanges();
            TempData["Success"] = $"Zone '{zone.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var zone = _db.GeoFenceZones.Find(id);
            if (zone is null) return NotFound();
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, GeoFenceZone model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

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
            TempData["Success"] = "Zone updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var zone = _db.GeoFenceZones.Find(id);
            if (zone is null) return NotFound();
            _db.GeoFenceZones.Remove(zone);
            _db.SaveChanges();
            TempData["Success"] = "Zone removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
