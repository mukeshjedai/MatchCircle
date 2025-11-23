using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class ProfileController : Controller
    {
        private readonly MatrimonialDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(MatrimonialDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userProfile = await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userProfile == null)
            {
                return RedirectToAction("Register", "Auth");
            }

            // Get all photos (including pending) so we can show the primary photo even if it's pending
            var photos = await _context.ProfilePhotos
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.IsPrimary ? 0 : 1) // Primary photos first
                .ThenBy(p => p.SortOrder)
                .ToListAsync();

            var partnerPreference = await _context.PartnerPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            ViewBag.Photos = photos;
            ViewBag.PartnerPreference = partnerPreference;
            ViewBag.Age = CalculateAge(userProfile.DateOfBirth);
            ViewBag.Height = FormatHeight(userProfile.HeightCm);
            
            // Store user name in session if not already set
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                HttpContext.Session.SetString("UserName", userProfile.FullName);
            }
            
            return View(userProfile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string section = "about")
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userProfile = await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userProfile == null)
            {
                return RedirectToAction("Register", "Auth");
            }

            ViewBag.Section = section;
            ViewBag.User = userProfile.User;
            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfile model, string section)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (model.UserId != userId)
            {
                return Forbid();
            }

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userProfile == null)
            {
                return NotFound();
            }

            // Update based on section
            switch (section?.ToLower())
            {
                case "about":
                    userProfile.AboutMe = model.AboutMe;
                    break;

                case "basics":
                    userProfile.DateOfBirth = model.DateOfBirth.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(model.DateOfBirth, DateTimeKind.Utc) 
                        : model.DateOfBirth.ToUniversalTime();
                    userProfile.MaritalStatus = model.MaritalStatus;
                    userProfile.HeightCm = model.HeightCm;
                    userProfile.Diet = model.Diet;
                    break;

                case "religious":
                    userProfile.Religion = model.Religion;
                    userProfile.Caste = model.Caste;
                    userProfile.SubCaste = model.SubCaste;
                    userProfile.MotherTongue = model.MotherTongue;
                    userProfile.Community = model.Community;
                    break;

                case "family":
                    userProfile.FamilyDetails = model.FamilyDetails;
                    break;

                case "education":
                    userProfile.EducationLevel = model.EducationLevel;
                    userProfile.EducationField = model.EducationField;
                    userProfile.Occupation = model.Occupation;
                    userProfile.Employer = model.Employer;
                    userProfile.AnnualIncomeCurrency = model.AnnualIncomeCurrency;
                    userProfile.AnnualIncomeMin = model.AnnualIncomeMin;
                    userProfile.AnnualIncomeMax = model.AnnualIncomeMax;
                    break;

                case "location":
                    userProfile.Country = model.Country;
                    userProfile.State = model.State;
                    userProfile.City = model.City;
                    userProfile.ResidenceStatus = model.ResidenceStatus;
                    userProfile.Citizenship = model.Citizenship;
                    break;

                case "contact":
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.PhoneCountryCode = model.User?.PhoneCountryCode;
                        user.PhoneNumber = model.User?.PhoneNumber;
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                    break;

                default:
                    // Update all fields
                    userProfile.FullName = model.FullName;
                    userProfile.Gender = model.Gender;
                    userProfile.DateOfBirth = model.DateOfBirth.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(model.DateOfBirth, DateTimeKind.Utc) 
                        : model.DateOfBirth.ToUniversalTime();
                    userProfile.MaritalStatus = model.MaritalStatus;
                    userProfile.HeightCm = model.HeightCm;
                    userProfile.Religion = model.Religion;
                    userProfile.Caste = model.Caste;
                    userProfile.SubCaste = model.SubCaste;
                    userProfile.MotherTongue = model.MotherTongue;
                    userProfile.Country = model.Country;
                    userProfile.State = model.State;
                    userProfile.City = model.City;
                    userProfile.EducationLevel = model.EducationLevel;
                    userProfile.EducationField = model.EducationField;
                    userProfile.Occupation = model.Occupation;
                    userProfile.Employer = model.Employer;
                    userProfile.AnnualIncomeCurrency = model.AnnualIncomeCurrency;
                    userProfile.AnnualIncomeMin = model.AnnualIncomeMin;
                    userProfile.AnnualIncomeMax = model.AnnualIncomeMax;
                    userProfile.Diet = model.Diet;
                    userProfile.AboutMe = model.AboutMe;
                    userProfile.FamilyDetails = model.FamilyDetails;
                    userProfile.ResidenceStatus = model.ResidenceStatus;
                    userProfile.Citizenship = model.Citizenship;
                    break;
            }

            userProfile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditPartnerPreferences()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var partnerPreference = await _context.PartnerPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partnerPreference == null)
            {
                partnerPreference = new PartnerPreference
                {
                    UserId = userId,
                    PreferredGender = "any"
                };
            }

            return View(partnerPreference);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartnerPreferences(PartnerPreference model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Always set UserId from session to ensure it's correct
            model.UserId = userId;
            
            // Ensure PreferredGender has a default value
            if (string.IsNullOrEmpty(model.PreferredGender))
            {
                model.PreferredGender = "any";
            }

            // Clear model state errors for fields we're handling manually
            ModelState.Remove("UserId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for partner preferences. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var existing = await _context.PartnerPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existing == null)
            {
                // Create new preference
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                _context.PartnerPreferences.Add(model);
            }
            else
            {
                // Update existing preference - only update fields that are in the form
                existing.AgeMin = model.AgeMin;
                existing.AgeMax = model.AgeMax;
                existing.HeightMinCm = model.HeightMinCm;
                existing.HeightMaxCm = model.HeightMaxCm;
                existing.PreferredGender = model.PreferredGender;
                // Don't overwrite JSON fields if they're not in the model (preserve existing values)
                // Only update if explicitly provided (not null)
                if (model.MaritalStatusesJson != null)
                    existing.MaritalStatusesJson = model.MaritalStatusesJson;
                if (model.ReligionsJson != null)
                    existing.ReligionsJson = model.ReligionsJson;
                if (model.MotherTonguesJson != null)
                    existing.MotherTonguesJson = model.MotherTonguesJson;
                if (model.CountriesJson != null)
                    existing.CountriesJson = model.CountriesJson;
                if (model.EducationLevelsJson != null)
                    existing.EducationLevelsJson = model.EducationLevelsJson;
                existing.IncomeMin = model.IncomeMin;
                existing.IncomeCurrency = model.IncomeCurrency;
                existing.OtherRequirements = model.OtherRequirements;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Partner preferences updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                _logger.LogError(ex, "Error saving partner preferences for user {UserId}", userId);
                
                ModelState.AddModelError("", $"An error occurred while saving your preferences: {ex.Message}. Please try again.");
                return View(model);
            }
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        private string FormatHeight(short? heightCm)
        {
            if (!heightCm.HasValue) return "Not Specified";
            var feet = heightCm.Value / 30.48;
            var inches = (feet - Math.Floor(feet)) * 12;
            return $"{Math.Floor(feet)}' {Math.Round(inches)}\" ({heightCm}cm)";
        }

        [HttpGet]
        public async Task<IActionResult> View(long id)
        {
            var profile = await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (profile == null)
            {
                return NotFound();
            }

            // Get all photos (approved and pending) - exclude deleted
            // Other users should be able to see all photos the user has uploaded
            var photos = await _context.ProfilePhotos
                .Where(p => p.UserId == id && p.Status != "deleted")
                .OrderBy(p => p.IsPrimary ? 0 : 1)
                .ThenBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            // Calculate age
            var age = CalculateAge(profile.DateOfBirth);
            var height = FormatHeight(profile.HeightCm);

            // Check if current user is connected to this profile
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            var isConnected = false;
            var hasPendingRequest = false;
            var pendingRequestSent = false;

            if (!string.IsNullOrEmpty(currentUserIdStr) && long.TryParse(currentUserIdStr, out var currentUserId))
            {
                if (currentUserId != id)
                {
                    // Check if they are connected (either direction)
                    isConnected = await _context.UserInteractions
                        .AnyAsync(ui => ui.InteractionType == "connect" && 
                                       ui.Status == "accepted" &&
                                       ((ui.FromUserId == currentUserId && ui.ToUserId == id) ||
                                        (ui.FromUserId == id && ui.ToUserId == currentUserId)));

                    // Check if there's a pending request
                    var pendingRequest = await _context.UserInteractions
                        .FirstOrDefaultAsync(ui => ui.InteractionType == "connect" && 
                                                   ui.Status == "pending" &&
                                                   ((ui.FromUserId == currentUserId && ui.ToUserId == id) ||
                                                    (ui.FromUserId == id && ui.ToUserId == currentUserId)));

                    if (pendingRequest != null)
                    {
                        hasPendingRequest = true;
                        pendingRequestSent = pendingRequest.FromUserId == currentUserId;
                    }
                }
            }

            ViewBag.Photos = photos;
            ViewBag.Age = age;
            ViewBag.Height = height;
            ViewBag.IsConnected = isConnected;
            ViewBag.HasPendingRequest = hasPendingRequest;
            ViewBag.PendingRequestSent = pendingRequestSent;
            
            return View(profile);
        }
    }
}
