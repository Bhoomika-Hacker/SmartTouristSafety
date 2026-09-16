using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Controllers
{
    [Authorize(Roles = "Admin,Authority,Operator")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vm = new DashboardViewModel
            {
                TotalTourists = _db.Tourists.Count(),
                ActiveTourists = _db.Tourists.Count(t => t.IsActive),
                TotalZones = _db.GeoFenceZones.Count(),
                RestrictedOrHighRiskZones = _db.GeoFenceZones.Count(z =>
                    z.RiskLevel == RiskLevel.HighRisk || z.RiskLevel == RiskLevel.Restricted),
                OpenIncidents = _db.Incidents.Count(i =>
                    i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed),
                CriticalIncidents = _db.Incidents.Count(i =>
                    i.Severity == IncidentSeverity.Critical &&
                    i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed),
                RecentIncidents = _db.Incidents
                    .Include(i => i.Tourist)
                    .OrderByDescending(i => i.ReportedAt)
                    .Take(8)
                    .ToList(),
                RecentAlerts = _db.AlertLogs
                    .Include(a => a.Tourist)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(8)
                    .ToList(),
                RecentBreaches = _db.LocationLogs
                    .Include(l => l.Tourist)
                    .Include(l => l.Zone)
                    .Where(l => l.IsGeoFenceBreach)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(8)
                    .ToList(),
                TotalExternalReports = _db.AreaReports.Count(),
                LastExternalReportIngestedAt = _db.AreaReports
                    .OrderByDescending(r => r.IngestedAt)
                    .Select(r => (DateTime?)r.IngestedAt)
                    .FirstOrDefault()
            };

            return View(vm);
        }
    }
}
