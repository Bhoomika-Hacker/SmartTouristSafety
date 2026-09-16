using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models.Enums;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _hasher;

        public AccountController(ApplicationDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = _db.AppUsers.FirstOrDefault(u => u.Username == model.Username);

            if (user is null || !_hasher.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role.ToString())
            };
            if (user.TouristId.HasValue)
            {
                claims.Add(new Claim("TouristId", user.TouristId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Tourists land on their own safety portal; staff land on the command-center dashboard.
            return user.Role == UserRole.Tourist
                ? RedirectToAction("Index", "TouristPortal")
                : RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
