using System.Security.Cryptography;
using System.Text;

namespace SmartTouristSafety.Services
{
    public interface IPasswordHasher
    {
        string Hash(string plainText);
        bool Verify(string plainText, string hash);
    }

    /// <summary>Simple salted SHA-256 hasher (kept dependency-free for this demo project).</summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const string Salt = "sts-static-salt-v1";

        public string Hash(string plainText)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(Salt + plainText);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public bool Verify(string plainText, string hash) => Hash(plainText) == hash;
    }
}
