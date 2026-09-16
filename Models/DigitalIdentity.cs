using System.ComponentModel.DataAnnotations;

namespace SmartTouristSafety.Models
{
    /// <summary>
    /// Represents one block in the simplified blockchain ledger that issues
    /// a tamper-evident Digital ID for a tourist. Each block references the
    /// hash of the previous block, forming a chain whose integrity can be
    /// re-verified at any time (see IBlockchainService.VerifyChain).
    /// </summary>
    public class DigitalIdentity
    {
        public int Id { get; set; }

        public int BlockIndex { get; set; }

        public int TouristId { get; set; }
        public Tourist? Tourist { get; set; }

        [Required]
        public string PreviousHash { get; set; } = string.Empty;

        [Required]
        public string CurrentHash { get; set; } = string.Empty;

        public long Nonce { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsRevoked { get; set; } = false;

        /// <summary>The exact payload that was hashed, kept for re-verification.</summary>
        public string Payload { get; set; } = string.Empty;
    }
}
