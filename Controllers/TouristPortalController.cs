using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;
using System.Security.Claims;

namespace SmartTouristSafety.Controllers
{
    /// <summary>
    /// Self-service safety portal for a signed-in tourist: shows their live
    /// zone/risk status (via browser geolocation + /api/location/ping), the
    /// nearest police station, an SOS button, an FAQ chatbot, and their own
    /// incident history. Access is restricted to the tourist's own record —
    /// they can never see another tourist's data.
    /// </summary>
    [Authorize(Roles = "Tourist")]
    public class TouristPortalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlockchainService _blockchainService;

        public TouristPortalController(ApplicationDbContext db, IBlockchainService blockchainService)
        {
            _db = db;
            _blockchainService = blockchainService;
        }

        public IActionResult Index()
        {
            var touristId = GetCurrentTouristId();
            if (touristId is null) return Forbid();

            var tourist = _db.Tourists
                .Include(t => t.DigitalIdentity)
                .FirstOrDefault(t => t.Id == touristId.Value);
            if (tourist is null) return NotFound();

            var vm = new TouristPortalViewModel
            {
                Tourist = tourist,
                Zones = _db.GeoFenceZones.Where(z => z.IsActive).OrderBy(z => z.Name).ToList(),
                MyIncidents = _db.Incidents
                    .Where(i => i.TouristId == touristId.Value)
                    .OrderByDescending(i => i.ReportedAt)
                    .ToList(),
                DigitalIdStatus = _blockchainService.VerifyDigitalId(touristId.Value)
            };

            return View(vm);
        }

        private int? GetCurrentTouristId()
        {
            var claim = User.FindFirst("TouristId");
            return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
