using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    /// <summary>A single GPS ping received from a tourist's device / app.</summary>
    public class LocationLog
    {
        public int Id { get; set; }

        public int TouristId { get; set; }
        public Tourist? Tourist { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? ZoneId { get; set; }
        public GeoFenceZone? Zone { get; set; }

        public bool IsInsideKnownZone { get; set; }

        public bool IsGeoFenceBreach { get; set; }

        /// <summary>0-100 score produced by the AI risk-scoring service.</summary>
        public int AiRiskScore { get; set; }

        public string? AiRiskCategory { get; set; }

        /// <summary>Live risk level computed automatically from nearby real reports (kept for audit even when no zone matched).</summary>
        public RiskLevel DynamicRiskLevel { get; set; }

        /// <summary>How many real reports (in-app + external) fed into the dynamic score at this point.</summary>
        public int ContributingReportCount { get; set; }
    }
}
