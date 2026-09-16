using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Services
{
    public interface IIncidentResponseService
    {
        Task<LocationPingResultDto> ProcessLocationPingAsync(LocationPingDto ping);
        Incident RaisePanicButton(PanicButtonDto dto);
        Incident UpdateIncidentStatus(UpdateIncidentStatusDto dto);
        AlertLog LogAlert(int touristId, AlertType type, string message, int? incidentId = null, int? zoneId = null);
    }

    /// <summary>
    /// Central "brain" of the system: consumes GPS pings, runs them through
    /// geo-fencing + the AI risk model, and automatically raises alerts /
    /// incidents when a tourist crosses a restricted boundary or their risk
    /// score crosses the configured threshold — this is the automated
    /// incident-response pipeline described in the project brief.
    /// </summary>
    public class IncidentResponseService : IIncidentResponseService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeoFencingService _geoFencingService;
        private readonly IAiRiskService _aiRiskService;
        private readonly IDynamicRiskZoneService _dynamicRiskZoneService;
        private readonly IPoliceStationService _policeStationService;
        private readonly IReverseGeocodingService _reverseGeocodingService;
        private readonly int _highRiskThreshold;
        private readonly int _criticalRiskThreshold;

        public IncidentResponseService(
            ApplicationDbContext db,
            IGeoFencingService geoFencingService,
            IAiRiskService aiRiskService,
            IDynamicRiskZoneService dynamicRiskZoneService,
            IPoliceStationService policeStationService,
            IReverseGeocodingService reverseGeocodingService,
            IConfiguration configuration)
        {
            _db = db;
            _geoFencingService = geoFencingService;
            _aiRiskService = aiRiskService;
            _dynamicRiskZoneService = dynamicRiskZoneService;
            _policeStationService = policeStationService;
            _reverseGeocodingService = reverseGeocodingService;
            _highRiskThreshold = configuration.GetValue<int>("AppSettings:HighRiskScoreThreshold", 70);
            _criticalRiskThreshold = configuration.GetValue<int>("AppSettings:CriticalRiskScoreThreshold", 90);
        }

        public async Task<LocationPingResultDto> ProcessLocationPingAsync(LocationPingDto ping)
        {
            var tourist = _db.Tourists.Find(ping.TouristId)
                ?? throw new InvalidOperationException($"Tourist {ping.TouristId} not found.");

            var (zone, _) = _geoFencingService.FindContainingZone(ping.Latitude, ping.Longitude);
            var isBreach = zone is not null && zone.RiskLevel == RiskLevel.Restricted;

            // Every coordinate gets a live, report-driven risk reading automatically —
            // this is what covers areas nobody has manually marked as a zone.
            var dynamicRisk = _dynamicRiskZoneService.ComputeRiskAt(ping.Latitude, ping.Longitude);

            var (score, category) = _aiRiskService.ComputeRiskScore(tourist, zone, isBreach, ping.Latitude, ping.Longitude);

            var log = new LocationLog
            {
                TouristId = tourist.Id,
                Latitude = ping.Latitude,
                Longitude = ping.Longitude,
                Timestamp = ping.Timestamp ?? DateTime.UtcNow,
                ZoneId = zone?.Id,
                IsInsideKnownZone = zone is not null,
                IsGeoFenceBreach = isBreach,
                AiRiskScore = score,
                AiRiskCategory = category,
                DynamicRiskLevel = dynamicRisk.Level,
                ContributingReportCount = dynamicRisk.ContributingReportCount
            };
            _db.LocationLogs.Add(log);

            tourist.LastCheckInAt = DateTime.UtcNow;

            var result = new LocationPingResultDto
            {
                Latitude = ping.Latitude,
                Longitude = ping.Longitude,
                IsInsideKnownZone = zone is not null,
                ZoneName = zone?.Name,
                ZoneRiskLevel = zone?.RiskLevel,
                IsGeoFenceBreach = isBreach,
                AiRiskScore = score,
                AiRiskCategory = category,
                DynamicRiskLevel = dynamicRisk.Level,
                DynamicRiskScore = dynamicRisk.Score,
                ContributingReportCount = dynamicRisk.ContributingReportCount,
                NearestReportDistanceMeters = dynamicRisk.NearestReportDistanceMeters
            };

            // Automatically resolve the nearest REAL police station for this exact coordinate
            var nearestStation = await _policeStationService.FindNearestAsync(ping.Latitude, ping.Longitude);
            if (nearestStation is not null)
            {
                result.NearestPoliceStationName = nearestStation.Name;
                result.NearestPoliceStationDistanceMeters = nearestStation.DistanceMeters;
                result.NearestPoliceStationPhone = nearestStation.Phone;
                result.NearestPoliceStationAddress = nearestStation.Address;
                result.NearestPoliceStationLatitude = nearestStation.Latitude;
                result.NearestPoliceStationLongitude = nearestStation.Longitude;
            }

            // Real place name for this exact coordinate — this is what the UI shows instead of
            // just "unmarked area" when there's no administrator-declared zone here.
            result.LocationName = await _reverseGeocodingService.GetPlaceNameAsync(ping.Latitude, ping.Longitude);

            // Automated incident-response pipeline
            if (isBreach)
            {
                var incident = CreateIncidentInternal(tourist.Id, IncidentType.GeoFenceBreach, IncidentSeverity.Critical,
                    $"Tourist entered restricted zone '{zone!.Name}'.", ping.Latitude, ping.Longitude);
                LogAlertInternal(tourist.Id, AlertType.GeoFenceBreach,
                    $"{tourist.FullName} entered restricted zone '{zone.Name}'.", incident.Id, zone.Id);
                result.IncidentAutoCreated = true;
                result.IncidentId = incident.Id;
            }
            else if (score >= _criticalRiskThreshold)
            {
                var incident = CreateIncidentInternal(tourist.Id, IncidentType.AnomalyDetected, IncidentSeverity.Critical,
                    $"AI model flagged critical risk score ({score}/100) — {category}.", ping.Latitude, ping.Longitude);
                LogAlertInternal(tourist.Id, AlertType.AnomalyDetected,
                    $"Critical anomaly risk ({score}/100) detected for {tourist.FullName}.", incident.Id, zone?.Id);
                result.IncidentAutoCreated = true;
                result.IncidentId = incident.Id;
            }
            else if (score >= _highRiskThreshold)
            {
                LogAlertInternal(tourist.Id, AlertType.AnomalyDetected,
                    $"Elevated risk score ({score}/100) detected for {tourist.FullName}.", null, zone?.Id);
            }
            else if (dynamicRisk.Level == RiskLevel.Restricted && dynamicRisk.ContributingReportCount >= 3)
            {
                // No administrator ever marked this spot as a zone, but enough real, recent
                // reports have piled up nearby that it's being treated as high risk anyway.
                var incident = CreateIncidentInternal(tourist.Id, IncidentType.AnomalyDetected, IncidentSeverity.High,
                    $"Area automatically flagged high-risk from {dynamicRisk.ContributingReportCount} recent real report(s) nearby (no zone was manually marked here).",
                    ping.Latitude, ping.Longitude);
                LogAlertInternal(tourist.Id, AlertType.AnomalyDetected,
                    $"{tourist.FullName} entered an automatically-detected high-risk area (based on {dynamicRisk.ContributingReportCount} nearby reports).", incident.Id, null);
                result.IncidentAutoCreated = true;
                result.IncidentId = incident.Id;
            }

            _db.SaveChanges();
            return result;
        }

        public Incident RaisePanicButton(PanicButtonDto dto)
        {
            var tourist = _db.Tourists.Find(dto.TouristId)
                ?? throw new InvalidOperationException($"Tourist {dto.TouristId} not found.");

            var incident = CreateIncidentInternal(tourist.Id, IncidentType.PanicButton, IncidentSeverity.Critical,
                string.IsNullOrWhiteSpace(dto.Message) ? "Tourist activated the SOS panic button." : dto.Message,
                dto.Latitude, dto.Longitude);

            LogAlertInternal(tourist.Id, AlertType.PanicButton,
                $"SOS PANIC BUTTON activated by {tourist.FullName}.", incident.Id, null);

            _db.SaveChanges();
            return incident;
        }

        public Incident UpdateIncidentStatus(UpdateIncidentStatusDto dto)
        {
            var incident = _db.Incidents.Find(dto.IncidentId)
                ?? throw new InvalidOperationException($"Incident {dto.IncidentId} not found.");

            incident.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.AssignedResponder)) incident.AssignedResponder = dto.AssignedResponder;
            if (!string.IsNullOrWhiteSpace(dto.ResolutionNotes)) incident.ResolutionNotes = dto.ResolutionNotes;

            if (dto.Status == IncidentStatus.Acknowledged && incident.AcknowledgedAt is null)
                incident.AcknowledgedAt = DateTime.UtcNow;

            if (dto.Status is IncidentStatus.Resolved or IncidentStatus.Closed && incident.ResolvedAt is null)
                incident.ResolvedAt = DateTime.UtcNow;

            _db.SaveChanges();
            return incident;
        }

        public AlertLog LogAlert(int touristId, AlertType type, string message, int? incidentId = null, int? zoneId = null)
        {
            var alert = LogAlertInternal(touristId, type, message, incidentId, zoneId);
            _db.SaveChanges();
            return alert;
        }

        private Incident CreateIncidentInternal(int touristId, IncidentType type, IncidentSeverity severity, string description, double? lat, double? lng)
        {
            var incident = new Incident
            {
                TouristId = touristId,
                Type = type,
                Severity = severity,
                Status = IncidentStatus.Reported,
                Description = description,
                Latitude = lat,
                Longitude = lng
            };
            _db.Incidents.Add(incident);
            _db.SaveChanges(); // ensures incident.Id is available for the linked alert
            return incident;
        }

        private AlertLog LogAlertInternal(int touristId, AlertType type, string message, int? incidentId, int? zoneId)
        {
            var alert = new AlertLog
            {
                TouristId = touristId,
                AlertType = type,
                Message = message,
                IncidentId = incidentId,
                ZoneId = zoneId
            };
            _db.AlertLogs.Add(alert);
            return alert;
        }
    }
}
