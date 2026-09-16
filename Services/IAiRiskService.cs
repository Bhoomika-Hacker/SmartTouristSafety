using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Services
{
    public interface IAiRiskService
    {
        (int score, string category) ComputeRiskScore(Tourist tourist, GeoFenceZone? currentZone, bool isGeoFenceBreach, double lat, double lng);
    }

    /// <summary>
    /// A lightweight, explainable "AI" anomaly/risk scoring model.
    /// In production this would call out to a trained ML model (e.g. an
    /// anomaly-detection model trained on historical movement patterns);
    /// here it is implemented as a transparent weighted-feature heuristic
    /// so the scoring logic stays auditable for a safety-critical system.
    ///
    /// Signals used:
    ///   1. Zone risk level (Safe/Caution/HighRisk/Restricted)
    ///   2. Whether the tourist has stepped outside every known safe zone
    ///   3. Sudden long silence since the last check-in (possible distress)
    ///   4. Movement far from any previously logged position (route deviation)
    ///   5. Time of day (night hours raise risk slightly in Caution/HighRisk zones)
    ///   6. Live, report-driven risk for the exact coordinate — computed automatically from
    ///      real nearby incidents/news/open-data reports, so a location doesn't need an
    ///      administrator to have manually drawn a zone there to factor into the score.
    /// </summary>
    public class AiRiskService : IAiRiskService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeoFencingService _geoFencingService;
        private readonly IDynamicRiskZoneService _dynamicRiskZoneService;

        public AiRiskService(ApplicationDbContext db, IGeoFencingService geoFencingService, IDynamicRiskZoneService dynamicRiskZoneService)
        {
            _db = db;
            _geoFencingService = geoFencingService;
            _dynamicRiskZoneService = dynamicRiskZoneService;
        }

        public (int score, string category) ComputeRiskScore(Tourist tourist, GeoFenceZone? currentZone, bool isGeoFenceBreach, double lat, double lng)
        {
            var score = 0;

            // 1. Base score from zone risk level (only applies where an administrator has
            //    actually drawn a zone; everywhere else this contributes nothing and signal 6
            //    below — the live, report-driven score — carries the area's risk instead)
            score += currentZone?.RiskLevel switch
            {
                RiskLevel.Safe => 5,
                RiskLevel.Caution => 30,
                RiskLevel.HighRisk => 55,
                RiskLevel.Restricted => 80,
                _ => 0
            };

            // 2. Outright geo-fence breach (e.g. entered a Restricted zone)
            if (isGeoFenceBreach) score += 25;

            // 3. Inactivity / silence since last check-in
            if (tourist.LastCheckInAt.HasValue)
            {
                var silence = DateTime.UtcNow - tourist.LastCheckInAt.Value;
                if (silence > TimeSpan.FromHours(6)) score += 25;
                else if (silence > TimeSpan.FromHours(2)) score += 12;
            }

            // 4. Sudden large jump from the last known position (possible spoofing,
            //    vehicle-assisted abduction, or a fast, unplanned route deviation)
            var lastLog = _db.LocationLogs
                .Where(l => l.TouristId == tourist.Id)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            if (lastLog is not null)
            {
                var jumpMeters = _geoFencingService.DistanceMeters(lastLog.Latitude, lastLog.Longitude, lat, lng);
                var minutesElapsed = Math.Max(1, (DateTime.UtcNow - lastLog.Timestamp).TotalMinutes);
                var impliedSpeedKmh = (jumpMeters / 1000.0) / (minutesElapsed / 60.0);

                if (impliedSpeedKmh > 150) score += 20; // implausible for a pedestrian tourist
                else if (impliedSpeedKmh > 80) score += 8;
            }

            // 5. Night-time modifier in riskier zones
            var hour = DateTime.UtcNow.Hour;
            var isNight = hour >= 22 || hour <= 5;
            if (isNight && currentZone?.RiskLevel is RiskLevel.Caution or RiskLevel.HighRisk)
            {
                score += 10;
            }

            // 6. Live risk computed from real reports near this exact coordinate — this is
            //    what covers every area nobody has manually marked as a zone.
            var dynamicRisk = _dynamicRiskZoneService.ComputeRiskAt(lat, lng);
            score += (int)Math.Round(dynamicRisk.Score * 0.5);

            score = Math.Clamp(score, 0, 100);

            var category = score switch
            {
                >= 90 => "Critical",
                >= 70 => "High",
                >= 40 => "Moderate",
                _ => "Normal"
            };

            return (score, category);
        }
    }
}
