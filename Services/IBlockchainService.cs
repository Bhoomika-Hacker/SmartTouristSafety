using System.Security.Cryptography;
using System.Text;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.ViewModels;

namespace SmartTouristSafety.Services
{
    public interface IBlockchainService
    {
        DigitalIdentity IssueDigitalId(int touristId, TimeSpan validity);
        DigitalIdVerificationResultDto VerifyDigitalId(int touristId);
        bool VerifyEntireChain();
        void RevokeDigitalId(int touristId);
    }

    /// <summary>
    /// Simulates a permissioned blockchain ledger for tourist Digital IDs.
    /// Each tourist's ID is one block; blocks are linked via SHA-256 hashes
    /// (PreviousHash -> CurrentHash), the same principle used by real
    /// blockchains, so any tampering with a past block is detectable because
    /// it breaks every hash that follows it.
    /// </summary>
    public class BlockchainService : IBlockchainService
    {
        private readonly ApplicationDbContext _db;
        private const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000";

        public BlockchainService(ApplicationDbContext db)
        {
            _db = db;
        }

        public DigitalIdentity IssueDigitalId(int touristId, TimeSpan validity)
        {
            var lastBlock = _db.DigitalIdentities
                .OrderByDescending(d => d.BlockIndex)
                .FirstOrDefault();

            var previousHash = lastBlock?.CurrentHash ?? GenesisHash;
            var blockIndex = (lastBlock?.BlockIndex ?? -1) + 1;
            var issuedAt = DateTime.UtcNow;
            var expiresAt = issuedAt.Add(validity);
            var nonce = 0L;

            var payload = BuildPayload(blockIndex, touristId, previousHash, issuedAt, expiresAt, nonce);
            var hash = ComputeHash(payload);

            var block = new DigitalIdentity
            {
                BlockIndex = blockIndex,
                TouristId = touristId,
                PreviousHash = previousHash,
                CurrentHash = hash,
                Nonce = nonce,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                Payload = payload,
                IsActive = true
            };

            _db.DigitalIdentities.Add(block);
            _db.SaveChanges();
            return block;
        }

        public DigitalIdVerificationResultDto VerifyDigitalId(int touristId)
        {
            var block = _db.DigitalIdentities
                .Where(d => d.TouristId == touristId)
                .OrderByDescending(d => d.BlockIndex)
                .FirstOrDefault();

            if (block is null)
            {
                return new DigitalIdVerificationResultDto
                {
                    IsValid = false,
                    IsChainIntact = false,
                    IsExpired = true,
                    IsRevoked = false
                };
            }

            var recomputed = ComputeHash(block.Payload);
            var chainIntact = recomputed == block.CurrentHash;
            var expired = DateTime.UtcNow > block.ExpiresAt;

            return new DigitalIdVerificationResultDto
            {
                IsValid = chainIntact && !expired && !block.IsRevoked,
                IsChainIntact = chainIntact,
                IsExpired = expired,
                IsRevoked = block.IsRevoked,
                CurrentHash = block.CurrentHash,
                IssuedAt = block.IssuedAt,
                ExpiresAt = block.ExpiresAt
            };
        }

        /// <summary>Walks the whole ledger and confirms every block's PreviousHash
        /// correctly matches the CurrentHash of the block issued before it.</summary>
        public bool VerifyEntireChain()
        {
            var blocks = _db.DigitalIdentities.OrderBy(d => d.BlockIndex).ToList();
            var expectedPrevious = GenesisHash;

            foreach (var block in blocks)
            {
                if (block.PreviousHash != expectedPrevious) return false;
                if (ComputeHash(block.Payload) != block.CurrentHash) return false;
                expectedPrevious = block.CurrentHash;
            }
            return true;
        }

        public void RevokeDigitalId(int touristId)
        {
            var block = _db.DigitalIdentities
                .Where(d => d.TouristId == touristId)
                .OrderByDescending(d => d.BlockIndex)
                .FirstOrDefault();

            if (block is null) return;
            block.IsRevoked = true;
            block.IsActive = false;
            _db.SaveChanges();
        }

        private static string BuildPayload(int blockIndex, int touristId, string previousHash, DateTime issuedAt, DateTime expiresAt, long nonce)
            => $"{blockIndex}|{touristId}|{previousHash}|{issuedAt:O}|{expiresAt:O}|{nonce}";

        private static string ComputeHash(string payload)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
