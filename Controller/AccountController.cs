using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System.Security.Claims;

namespace SWD.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryManagementSystemContext _ctx;
        public AccountController(LibraryManagementSystemContext ctx) => _ctx = ctx;

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please fill all fields.";
                return View();
            }

            var exists = await _ctx.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                ViewBag.Error = "Email already exists.";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Password = password,  
                Role = "Member",
                Status = "Active"
            };
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();

            await SignIn(user, false);
            return RedirectToAction("Index", "Books");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            var user = await _ctx.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Wrong email or password.";
                return View();
            }

            await SignIn(user, rememberMe);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Books");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Sign out from authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Clear all cookies to ensure complete logout
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key);
            }
            
            // Redirect to Book List page
            return RedirectToAction("Index", "Books");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            // Sign out from authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Clear all cookies to ensure complete logout
            foreach (var key in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(key);
            }
            
            // Redirect to Book List page
            return RedirectToAction("Index", "Books");
        }


        private async Task SignIn(User user, bool remember)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Member")
            };
            var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(id),
                new AuthenticationProperties { IsPersistent = remember });
        }
    }
}
