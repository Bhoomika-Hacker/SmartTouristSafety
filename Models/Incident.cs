using System.ComponentModel.DataAnnotations;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    public class Incident
    {
        public int Id { get; set; }

        public int TouristId { get; set; }
        public Tourist? Tourist { get; set; }

        public IncidentType Type { get; set; }

        public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

        public IncidentStatus Status { get; set; } = IncidentStatus.Reported;

        [StringLength(1000)]
        public string? Description { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcknowledgedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [StringLength(120)]
        public string? AssignedResponder { get; set; }

        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }
    }
}
