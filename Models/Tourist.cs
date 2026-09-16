using System.ComponentModel.DataAnnotations;

namespace SmartTouristSafety.Models
{
    public class Tourist
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(40)]
        public string PassportOrIdNumber { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Nationality { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required, StringLength(120)]
        public string EmergencyContactName { get; set; } = string.Empty;

        [Required, Phone]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime TripStartDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        public DateTime TripEndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastCheckInAt { get; set; }

        // Navigation
        public DigitalIdentity? DigitalIdentity { get; set; }
        public List<LocationLog> LocationLogs { get; set; } = new();
        public List<Incident> Incidents { get; set; } = new();
        public List<AlertLog> AlertLogs { get; set; } = new();
    }
}
