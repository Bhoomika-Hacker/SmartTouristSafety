using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    /// <summary>Every automated / manual alert raised by the system is logged here.</summary>
    public class AlertLog
    {
        public int Id { get; set; }

        public int TouristId { get; set; }
        public Tourist? Tourist { get; set; }

        public int? IncidentId { get; set; }
        public Incident? Incident { get; set; }

        public int? ZoneId { get; set; }
        public GeoFenceZone? Zone { get; set; }

        public AlertType AlertType { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsAcknowledged { get; set; }
    }
}
