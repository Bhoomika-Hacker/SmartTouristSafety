using System.ComponentModel.DataAnnotations;

namespace SmartTouristSafety.Models
{
    public class PoliceStation
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Address { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
