using System.ComponentModel.DataAnnotations;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    /// <summary>
    /// A real-world safety report automatically ingested from an external source
    /// (a news feed, a police/government open-data portal, etc.) and tied to a
    /// location rather than to any specific registered tourist. This is the raw
    /// signal the <see cref="Services.IDynamicRiskZoneService"/> aggregates to
    /// compute the risk of an area on the fly — nobody has to draw a zone on a
    /// map by hand for the area to show up as risky.
    /// </summary>
    public class AreaReport
    {
        public int Id { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public ReportSource Source { get; set; }

        /// <summary>Human-readable name of the feed/provider, e.g. "GDELT News Feed".</summary>
        [StringLength(120)]
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Stable identifier from the origin system, used to de-duplicate on every
        /// sync so the same real-world event isn't counted twice.
        /// </summary>
        [StringLength(300)]
        public string ExternalReference { get; set; } = string.Empty;

        /// <summary>When the underlying real-world event/report happened, per the source.</summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>When our system pulled this report in.</summary>
        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Url { get; set; }
    }
}
