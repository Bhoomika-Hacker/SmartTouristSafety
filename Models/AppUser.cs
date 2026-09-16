using System.ComponentModel.DataAnnotations;
using SmartTouristSafety.Models.Enums;

namespace SmartTouristSafety.Models
{
    /// <summary>Application user. Admin/Authority/Operator use this for the command
    /// center dashboard; Tourist-role accounts are linked to a Tourist record and
    /// use it to sign in to the self-service safety portal.</summary>
    public class AppUser
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string DisplayName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Operator;

        /// <summary>Set only for Role == Tourist: links this login to their Tourist profile.</summary>
        public int? TouristId { get; set; }
        public Tourist? Tourist { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
