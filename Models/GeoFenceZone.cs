using System.ComponentModel.DataAnnotations;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    public class GeoFenceZone
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Description { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 500000)]
        public double RadiusMeters { get; set; } = 1000;

        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
