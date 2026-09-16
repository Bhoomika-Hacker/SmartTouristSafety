using SmartTouristSafety.Data;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Services
{
    public class DynamicRiskResult
    {
        /// <summary>0-100 computed score, same scale as the AI risk score.</summary>
        public int Score { get; set; }
        public RiskLevel Level { get; set; }
        public int ContributingReportCount { get; set; }
        public double RadiusMeters { get; set; }
        public double? NearestReportDistanceMeters { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IDynamicRiskZoneService
    {
        /// <summary>
        /// Computes a risk level for any coordinate purely from real reports near it —
        /// no administrator has to have drawn a zone there first.
        /// </summary>
        DynamicRiskResult ComputeRiskAt(double lat, double lng, double? radiusMetersOverride = null);
    }

    /// <summary>
    /// Replaces "an officer has to draw a zone on the map before we know it's risky" with
    /// live computation: every real report near a coordinate — a tourist-filed incident, an
    /// officer-filed one, or one auto-ingested from an external feed — contributes to that
    /// coordinate's risk score, weighted by how severe, how recent, and how close it is.
    /// A brand-new, never-before-flagged street can still come back "High Risk" the moment
    /// enough real reports show up nearby; a quiet area with a stale, sparse history still
    /// shows Safe even if nobody has ever explicitly marked it.
    /// </summary>
    public class DynamicRiskZoneService : IDynamicRiskZoneService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeoFencingService _geoFencingService;
        private readonly double _defaultRadiusMeters;
        private readonly int _lookbackDays;

        public DynamicRiskZoneService(ApplicationDbContext db, IGeoFencingService geoFencingService, IConfiguration configuration)
        {
            _db = db;
            _geoFencingService = geoFencingService;
            _defaultRadiusMeters = configuration.GetValue<double>("DynamicRisk:RadiusMeters", 2000.0);
            _lookbackDays = configuration.GetValue<int>("DynamicRisk:LookbackDays", 30);
        }

        public DynamicRiskResult ComputeRiskAt(double lat, double lng, double? radiusMetersOverride = null)
        {
            var radius = radiusMetersOverride ?? _defaultRadiusMeters;
            var cutoff = DateTime.UtcNow.AddDays(-_lookbackDays);

            var weighted = new List<(double weight, double distance)>();
            double? nearest = null;

            // Signal 1: real incidents already logged in-app (tourist SOS, officer-filed, auto-detected anomalies)
            var incidents = _db.Incidents
                .Where(i => i.Latitude != null && i.Longitude != null && i.ReportedAt >= cutoff)
                .ToList();

            foreach (var incident in incidents)
            {
                var distance = _geoFencingService.DistanceMeters(lat, lng, incident.Latitude!.Value, incident.Longitude!.Value);
                if (distance > radius) continue;

                nearest = nearest is null ? distance : Math.Min(nearest.Value, distance);
                var ageDays = (DateTime.UtcNow - incident.ReportedAt).TotalDays;
                weighted.Add((Weight(incident.Severity, ageDays, distance, radius), distance));
            }

            // Signal 2: real reports pulled in automatically from external sources (news, open crime data, etc.)
            var externalReports = _db.AreaReports
                .Where(r => r.OccurredAt >= cutoff)
                .ToList();

            foreach (var report in externalReports)
            {
                var distance = _geoFencingService.DistanceMeters(lat, lng, report.Latitude, report.Longitude);
                if (distance > radius) continue;

                nearest = nearest is null ? distance : Math.Min(nearest.Value, distance);
                var ageDays = (DateTime.UtcNow - report.OccurredAt).TotalDays;
                weighted.Add((Weight(report.Severity, ageDays, distance, radius), distance));
            }

            var totalWeight = weighted.Sum(w => w.weight);

            // Saturating curve: a handful of serious/recent/close reports quickly pushes the
            // score up, but it can never exceed 100 no matter how many reports pile on.
            var score = (int)Math.Clamp(Math.Round(100 * (1 - Math.Exp(-totalWeight / 50.0))), 0, 100);

            var level = score switch
            {
                >= 75 => RiskLevel.Restricted,
                >= 50 => RiskLevel.HighRisk,
                >= 20 => RiskLevel.Caution,
                _ => RiskLevel.Safe
            };

            return new DynamicRiskResult
            {
                Score = score,
                Level = level,
                ContributingReportCount = weighted.Count,
                RadiusMeters = radius,
                NearestReportDistanceMeters = nearest
            };
        }

        /// <summary>Severity x recency-decay x proximity-decay — closer, fresher, and more severe reports count more.</summary>
        private static double Weight(IncidentSeverity severity, double ageDays, double distanceMeters, double radiusMeters)
        {
            var severityWeight = severity switch
            {
                IncidentSeverity.Low => 10.0,
                IncidentSeverity.Medium => 20.0,
                IncidentSeverity.High => 35.0,
                IncidentSeverity.Critical => 55.0,
                _ => 15.0
            };

            // ~14-day half-life: a report from two weeks ago counts for about half as much as a fresh one.
            var recencyDecay = Math.Exp(-Math.Max(0, ageDays) / 14.0);

            // Linear falloff to the edge of the search radius.
            var proximityDecay = Math.Clamp(1.0 - (distanceMeters / Math.Max(1, radiusMeters)), 0.0, 1.0);

            return severityWeight * recencyDecay * proximityDecay;
        }
    }
}
