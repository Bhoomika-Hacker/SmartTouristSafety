using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/incidents")]
    public class IncidentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IIncidentResponseService _incidentResponseService;

        public IncidentsApiController(ApplicationDbContext db, IIncidentResponseService incidentResponseService)
        {
            _db = db;
            _incidentResponseService = incidentResponseService;
        }

        // GET api/incidents?status=Reported
        [HttpGet]
        public ActionResult<IEnumerable<Incident>> GetAll([FromQuery] string? status = null)
        {
            var query = _db.Incidents.Include(i => i.Tourist).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<Models.Enums.IncidentStatus>(status, true, out var parsed))
            {
                query = query.Where(i => i.Status == parsed);
            }

            return Ok(query.OrderByDescending(i => i.ReportedAt).ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Incident> GetById(int id)
        {
            var incident = _db.Incidents.Include(i => i.Tourist).FirstOrDefault(i => i.Id == id);
            return incident is null ? NotFound() : Ok(incident);
        }

        // POST api/incidents  -> manually report an incident
        [HttpPost]
        public ActionResult<Incident> Create(Incident incident)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _db.Incidents.Add(incident);
            _db.SaveChanges();

            _incidentResponseService.LogAlert(incident.TouristId, Models.Enums.AlertType.AnomalyDetected,
                $"Incident reported via API: {incident.Type} ({incident.Severity}).", incident.Id);

            return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
        }

        // PUT api/incidents/status  -> update workflow status (Reported -> Acknowledged -> Dispatched -> Resolved -> Closed)
        [HttpPut("status")]
        public ActionResult<Incident> UpdateStatus(UpdateIncidentStatusDto dto)
        {
            try
            {
                var updated = _incidentResponseService.UpdateIncidentStatus(dto);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST api/incidents/panic  -> tourist app / portal SOS button
        [HttpPost("panic")]
        public ActionResult<Incident> Panic(PanicButtonDto dto)
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
                var incident = _incidentResponseService.RaisePanicButton(dto);
                return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
