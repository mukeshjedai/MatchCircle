using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class SearchController : Controller
    {
        private readonly MatrimonialDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(MatrimonialDbContext context, ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? gender, string? religion, string? city, int? ageMin, int? ageMax)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get current user's profile to determine search criteria
            var currentProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (currentProfile == null)
            {
                return RedirectToAction("Index", "Profile");
            }

            // Build query - exclude current user
            var query = _context.UserProfiles
                .Include(p => p.User)
                .Where(p => p.UserId != userId);
            
            // Filter by active status (or null status to include all users)
            // Only exclude if status is explicitly set to something other than "active"
            query = query.Where(p => p.User == null || p.User.Status == "active" || string.IsNullOrEmpty(p.User.Status));

            // Apply gender filter only if specified
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }
            else
            {
                // If no gender specified, default to opposite gender (but make it optional)
                // For now, show all genders if not specified
                // Uncomment below to default to opposite gender:
                // var defaultGender = currentProfile.Gender == "Male" ? "Female" : "Male";
                // query = query.Where(p => p.Gender == defaultGender);
            }

            // Apply filters with case-insensitive matching
            if (!string.IsNullOrEmpty(religion))
            {
                query = query.Where(p => p.Religion != null && EF.Functions.ILike(p.Religion, religion));
            }

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(p => p.City != null && EF.Functions.ILike(p.City, city));
            }

            // Age filters
            if (ageMin.HasValue)
            {
                var maxDate = DateTime.UtcNow.AddYears(-ageMin.Value);
                query = query.Where(p => p.DateOfBirth <= maxDate);
            }

            if (ageMax.HasValue)
            {
                var minDate = DateTime.UtcNow.AddYears(-ageMax.Value - 1);
                query = query.Where(p => p.DateOfBirth >= minDate);
            }

            var profiles = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            // Load primary photos for all profiles
            var profileUserIds = profiles.Select(p => p.UserId).ToList();
            var photos = await _context.ProfilePhotos
                .Where(p => profileUserIds.Contains(p.UserId) && p.IsPrimary && p.Status != "deleted")
                .ToListAsync();

            var photosDict = photos.ToDictionary(p => p.UserId);

            // Log for debugging
            _logger.LogInformation("Search query returned {Count} profiles. Filters: Gender={Gender}, Religion={Religion}, City={City}, AgeMin={AgeMin}, AgeMax={AgeMax}", 
                profiles.Count, gender, religion, city, ageMin, ageMax);

            ViewBag.Gender = gender;
            ViewBag.Religion = religion;
            ViewBag.City = city;
            ViewBag.AgeMin = ageMin;
            ViewBag.AgeMax = ageMax;
            ViewBag.Photos = photosDict;

            return View(profiles);
        }
    }
}




