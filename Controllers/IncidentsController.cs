using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers
{
    [Authorize(Roles = "Admin,Authority,Operator")]
    public class IncidentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IIncidentResponseService _incidentResponseService;

        public IncidentsController(ApplicationDbContext db, IIncidentResponseService incidentResponseService)
        {
            _db = db;
            _incidentResponseService = incidentResponseService;
        }

        public IActionResult Index()
        {
            var incidents = _db.Incidents
                .Include(i => i.Tourist)
                .OrderByDescending(i => i.ReportedAt)
                .ToList();
            return View(incidents);
        }

        public IActionResult Details(int id)
        {
            var incident = _db.Incidents
                .Include(i => i.Tourist)
                .FirstOrDefault(i => i.Id == id);
            if (incident is null) return NotFound();
            return View(incident);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Tourists = _db.Tourists.OrderBy(t => t.FullName).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.Incident incident)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tourists = _db.Tourists.OrderBy(t => t.FullName).ToList();
                return View(incident);
            }

            _db.Incidents.Add(incident);
            _db.SaveChanges();

            _incidentResponseService.LogAlert(incident.TouristId, Models.Enums.AlertType.AnomalyDetected,
                $"Manually reported incident: {incident.Type} ({incident.Severity}).", incident.Id);

            TempData["Success"] = "Incident logged and responders notified.";
            return RedirectToAction(nameof(Details), new { id = incident.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(UpdateIncidentStatusDto dto)
        {
            _incidentResponseService.UpdateIncidentStatus(dto);
            TempData["Success"] = "Incident status updated.";
            return RedirectToAction(nameof(Details), new { id = dto.IncidentId });
        }
    }
}
