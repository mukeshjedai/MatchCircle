using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MatrimonialDbContext _context;

        public HomeController(ILogger<HomeController> logger, MatrimonialDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                // Show public landing page for non-logged-in users
                return View();
            }

            // Get user profile
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var profile = user.UserProfile;
            // Get all photos (including pending) so we can show the primary photo even if it's pending
            var photos = await _context.ProfilePhotos
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.IsPrimary ? 0 : 1)
                .ThenBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            // Calculate age
            var age = 0;
            if (profile != null)
            {
                age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;
                if (DateTime.UtcNow.DayOfYear < profile.DateOfBirth.DayOfYear)
                    age--;
            }

            // Get activity stats
            var pendingInvitations = await _context.UserInteractions
                .CountAsync(ui => ui.ToUserId == userId && ui.InteractionType == "interest" && 
                    !_context.UserInteractions.Any(ui2 => ui2.FromUserId == userId && ui2.ToUserId == ui.FromUserId && ui2.InteractionType == "interest"));

            var acceptedInvitations = await _context.Matches
                .CountAsync(m => (m.User1Id == userId || m.User2Id == userId) && m.Status == "active");

            var recentVisitors = await _context.UserInteractions
                .Where(ui => ui.ToUserId == userId && ui.InteractionType == "view")
                .OrderByDescending(ui => ui.CreatedAt)
                .Take(10)
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .ToListAsync();

            // Get matches
            var matches = await _context.Matches
                .Where(m => (m.User1Id == userId || m.User2Id == userId) && m.Status == "active")
                .Include(m => m.User1)
                .ThenInclude(u => u.UserProfile)
                .Include(m => m.User2)
                .ThenInclude(u => u.UserProfile)
                .ToListAsync();

            var newMatches = new List<MatchViewModel>();
            foreach (var match in matches.Take(5))
            {
                var otherUser = match.User1Id == userId ? match.User2 : match.User1;
                var otherProfile = otherUser.UserProfile;
                var otherPhotos = await _context.ProfilePhotos
                    .Where(p => p.UserId == otherUser.Id && p.IsPrimary && p.Status == "approved")
                    .FirstOrDefaultAsync();

                // Format height
                string heightStr = "Not specified";
                if (otherProfile?.HeightCm.HasValue == true)
                {
                    var cm = otherProfile.HeightCm.Value;
                    var feet = cm / 30.48;
                    var inches = (feet - Math.Floor(feet)) * 12;
                    heightStr = $"{(int)Math.Floor(feet)}'{(int)Math.Round(inches)}\"";
                }

                newMatches.Add(new MatchViewModel
                {
                    UserId = otherUser.Id,
                    Name = otherProfile?.FullName ?? otherUser.Email,
                    Age = otherProfile != null ? DateTime.UtcNow.Year - otherProfile.DateOfBirth.Year : 0,
                    Height = heightStr,
                    City = otherProfile?.City ?? "Not specified",
                    Occupation = otherProfile?.Occupation ?? "Not specified",
                    MotherTongue = otherProfile?.MotherTongue ?? "Not specified",
                    PhotoUrl = otherPhotos?.ImageUrl
                });
            }

            // Get notifications (recent interactions)
            var notifications = await _context.UserInteractions
                .Where(ui => ui.ToUserId == userId)
                .OrderByDescending(ui => ui.CreatedAt)
                .Take(10)
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .ToListAsync();

            // Get notification photos
            var notificationPhotos = new Dictionary<long, string?>();
            foreach (var notif in notifications)
            {
                var photo = await _context.ProfilePhotos
                    .Where(p => p.UserId == notif.FromUserId && p.IsPrimary && p.Status == "approved")
                    .Select(p => p.ImageUrl)
                    .FirstOrDefaultAsync();
                notificationPhotos[notif.FromUserId] = photo;
            }

            // Get visitor photos
            var visitorPhotos = new Dictionary<long, string?>();
            foreach (var visitor in recentVisitors)
            {
                var photo = await _context.ProfilePhotos
                    .Where(p => p.UserId == visitor.FromUserId && p.IsPrimary && p.Status == "approved")
                    .Select(p => p.ImageUrl)
                    .FirstOrDefaultAsync();
                visitorPhotos[visitor.FromUserId] = photo;
            }

            // Get accepted connections (My Matches)
            var myMatches = await _context.UserInteractions
                .Where(ui => (ui.FromUserId == userId || ui.ToUserId == userId) && 
                            ui.InteractionType == "connect" && 
                            ui.Status == "accepted")
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .Include(ui => ui.ToUser)
                .ThenInclude(u => u.UserProfile)
                .OrderByDescending(ui => ui.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Get match photos
            var matchPhotos = new Dictionary<long, string?>();
            foreach (var match in myMatches)
            {
                var otherUserId = match.FromUserId == userId ? match.ToUserId : match.FromUserId;
                var photo = await _context.ProfilePhotos
                    .Where(p => p.UserId == otherUserId && p.IsPrimary && p.Status != "deleted")
                    .Select(p => p.ImageUrl)
                    .FirstOrDefaultAsync();
                matchPhotos[otherUserId] = photo;
            }

            // Get unread message count
            var unreadMessages = await _context.Messages
                .CountAsync(m => m.ToUserId == userId && !m.IsRead);

            ViewBag.User = user;
            ViewBag.Profile = profile;
            ViewBag.Photos = photos;
            ViewBag.Age = age;
            ViewBag.PendingInvitations = pendingInvitations;
            ViewBag.AcceptedInvitations = acceptedInvitations;
            ViewBag.RecentVisitors = recentVisitors;
            ViewBag.NewMatches = newMatches;
            ViewBag.Notifications = notifications;
            ViewBag.NotificationPhotos = notificationPhotos;
            ViewBag.VisitorPhotos = visitorPhotos;
            ViewBag.MyMatches = myMatches;
            ViewBag.MatchPhotos = matchPhotos;
            ViewBag.UnreadMessages = unreadMessages;

            return View();
        }

        public class MatchViewModel
        {
            public long UserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Height { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Occupation { get; set; } = string.Empty;
            public string MotherTongue { get; set; } = string.Empty;
            public string? PhotoUrl { get; set; }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Development endpoint to manually seed Kali user and messages
        [HttpGet]
        public async Task<IActionResult> SeedKali()
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                await DbSeeder.SeedKaliUserAndMessagesAsync(_context);
                return Json(new { success = true, message = "Kali user and messages seeded successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Kali user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [IgnoreAntiforgeryToken]
        public IActionResult Error()
        {
            ViewData["RequestId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}

