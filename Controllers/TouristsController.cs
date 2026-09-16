using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers
{
    [Authorize(Roles = "Admin,Authority,Operator")]
    public class TouristsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlockchainService _blockchainService;
        private readonly IPasswordHasher _hasher;

        public TouristsController(ApplicationDbContext db, IBlockchainService blockchainService, IPasswordHasher hasher)
        {
            _db = db;
            _blockchainService = blockchainService;
            _hasher = hasher;
        }

        public IActionResult Index()
        {
            var tourists = _db.Tourists
                .Include(t => t.DigitalIdentity)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
            return View(tourists);
        }

        public IActionResult Details(int id)
        {
            var tourist = _db.Tourists
                .Include(t => t.DigitalIdentity)
                .Include(t => t.Incidents)
                .Include(t => t.LocationLogs.OrderByDescending(l => l.Timestamp).Take(20))
                .ThenInclude(l => l.Zone)
                .FirstOrDefault(t => t.Id == id);

            if (tourist is null) return NotFound();

            ViewBag.DigitalIdVerification = _blockchainService.VerifyDigitalId(id);
            ViewBag.PortalLoginUsername = _db.AppUsers
                .Where(u => u.TouristId == id)
                .Select(u => u.Username)
                .FirstOrDefault();
            return View(tourist);
        }

        [HttpGet]
        public IActionResult Create() => View(new Tourist());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tourist tourist)
        {
            if (!ModelState.IsValid) return View(tourist);

            _db.Tourists.Add(tourist);
            _db.SaveChanges();

            // Automatically issue a blockchain-based digital ID valid for the trip duration (+1 day buffer)
            var validity = (tourist.TripEndDate - tourist.TripStartDate).Add(TimeSpan.FromDays(1));
            if (validity <= TimeSpan.Zero) validity = TimeSpan.FromDays(7);
            _blockchainService.IssueDigitalId(tourist.Id, validity);

            TempData["Success"] = $"Tourist '{tourist.FullName}' registered and a Digital ID has been issued.";
            return RedirectToAction(nameof(Details), new { id = tourist.Id });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();
            return View(tourist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Tourist model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();

            tourist.FullName = model.FullName;
            tourist.PassportOrIdNumber = model.PassportOrIdNumber;
            tourist.Nationality = model.Nationality;
            tourist.Phone = model.Phone;
            tourist.Email = model.Email;
            tourist.EmergencyContactName = model.EmergencyContactName;
            tourist.EmergencyContactPhone = model.EmergencyContactPhone;
            tourist.TripStartDate = model.TripStartDate;
            tourist.TripEndDate = model.TripEndDate;
            tourist.IsActive = model.IsActive;

            _db.SaveChanges();
            TempData["Success"] = "Tourist details updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();
            _db.Tourists.Remove(tourist);
            _db.SaveChanges();
            TempData["Success"] = "Tourist record removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RevokeDigitalId(int id)
        {
            _blockchainService.RevokeDigitalId(id);
            TempData["Success"] = "Digital ID revoked.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Creates a self-service portal login for this tourist so they can see
        /// their own zone/risk status, nearest police station, use the SOS
        /// button, and chat with the safety assistant. Generates a one-time
        /// password shown to the admin to hand over to the tourist.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateLogin(int id)
        {
            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();

            if (_db.AppUsers.Any(u => u.TouristId == id))
            {
                TempData["Success"] = "This tourist already has a portal login.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var baseUsername = new string(tourist.PassportOrIdNumber.ToLowerInvariant()
                .Where(char.IsLetterOrDigit).ToArray());
            var username = baseUsername;
            var suffix = 1;
            while (_db.AppUsers.Any(u => u.Username == username))
            {
                username = $"{baseUsername}{suffix++}";
            }

            var tempPassword = "Tour" + Random.Shared.Next(100000, 999999);

            _db.AppUsers.Add(new AppUser
            {
                Username = username,
                DisplayName = tourist.FullName,
                PasswordHash = _hasher.Hash(tempPassword),
                Role = UserRole.Tourist,
                TouristId = tourist.Id
            });
            _db.SaveChanges();

            TempData["Success"] = $"Portal login created — Username: {username} / Temporary Password: {tempPassword}. " +
                                   "Share these with the tourist now; they won't be shown again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
