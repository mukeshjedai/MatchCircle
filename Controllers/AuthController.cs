using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using testapp1.Data;
using testapp1.Models;
using System.ComponentModel.DataAnnotations;

namespace testapp1.Controllers
{
    public class AuthController : Controller
    {
        private readonly MatrimonialDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(MatrimonialDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(string.Empty, "Email already registered.");
                return View(model);
            }

            // Hash password
            var passwordHash = HashPassword(model.Password);

            // Create user
            var user = new User
            {
                Email = model.Email,
                PasswordHash = passwordHash,
                Status = "active",
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create user profile
            var dateOfBirth = model.DateOfBirth.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(model.DateOfBirth, DateTimeKind.Utc) 
                : model.DateOfBirth.ToUniversalTime();

            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = model.FullName,
                Gender = model.Gender,
                DateOfBirth = dateOfBirth,
                MaritalStatus = model.MaritalStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", new { registered = true });
        }

        [HttpGet]
        public IActionResult Login(bool? registered = null)
        {
            if (registered == true)
            {
                ViewData["Message"] = "Registration successful! Please login.";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (user.Status != "active")
            {
                ModelState.AddModelError(string.Empty, "Your account is not active.");
                return View(model);
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create session token
            var token = Guid.NewGuid().ToString();
            var tokenHash = HashPassword(token);

            var session = new UserLoginSession
            {
                UserId = user.Id,
                AuthTokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserLoginSessions.Add(session);
            await _context.SaveChangesAsync();

            // Get user's profile name
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            var displayName = userProfile?.FullName ?? user.Email;

            // Store user ID and name in session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("AuthToken", token);
            HttpContext.Session.SetString("UserName", displayName);

            // Set persistent cookies for session restoration
            SetPersistentCookies(user.Id.ToString(), token);

            // Create claims and sign in with cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Profile");
        }

        // Google Login
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google Login Callback
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                return RedirectToAction("Login", new { error = "Google authentication failed." });
            }

            var claims = result.Principal?.Claims.ToList();
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", new { error = "Unable to retrieve email from Google account." });
            }

            // Find or create user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            
            var isNewUser = user == null;
            UserProfile? createdProfile = null;

            if (user == null)
            {
                // Create new user from Google account
                user = new User
                {
                    Email = email,
                    PasswordHash = HashPassword(Guid.NewGuid().ToString()), // Random hash for Google users
                    Status = "active",
                    Role = "user",
                    IsEmailVerified = true, // Google email is verified
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create basic profile
                createdProfile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = name ?? email.Split('@')[0],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserProfiles.Add(createdProfile);
                await _context.SaveChangesAsync();
            }

            if (user.Status != "active")
            {
                return RedirectToAction("Login", new { error = "Your account is not active." });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create session token
            var token = Guid.NewGuid().ToString();
            var tokenHash = HashPassword(token);

            var session = new UserLoginSession
            {
                UserId = user.Id,
                AuthTokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserLoginSessions.Add(session);
            await _context.SaveChangesAsync();

            // Get user's profile name (use created profile if new user, otherwise fetch from DB)
            var userProfile = createdProfile ?? await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            var displayName = userProfile?.FullName ?? name ?? email.Split('@')[0];

            // Store user ID and name in session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("AuthToken", token);
            HttpContext.Session.SetString("UserName", displayName);

            // Set persistent cookies
            SetPersistentCookies(user.Id.ToString(), token);

            // Sign in with cookie authentication
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Note: We don't need to sign out from Google scheme - it's only used for authentication
            // The cookie authentication scheme handles our session management

            // Set welcome message
            if (isNewUser)
            {
                TempData["WelcomeMessage"] = $"Welcome, {displayName}! Your account has been created. Please complete your profile information.";
            }
            else
            {
                TempData["WelcomeMessage"] = $"Welcome back, {displayName}! You have successfully signed in with Google.";
            }

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Clear persistent cookies
            Response.Cookies.Delete(".MatchCircle.AuthToken");
            Response.Cookies.Delete(".MatchCircle.UserId");

            // Sign out from cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        private void SetPersistentCookies(string userId, string authToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                IsEssential = true
            };

            Response.Cookies.Append(".MatchCircle.UserId", userId, cookieOptions);
            Response.Cookies.Append(".MatchCircle.AuthToken", authToken, cookieOptions);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Marital Status")]
        public string MaritalStatus { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}

