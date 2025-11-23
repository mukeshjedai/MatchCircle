using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class ConnectController : Controller
    {
        private readonly MatrimonialDbContext _context;
        private readonly ILogger<ConnectController> _logger;

        public ConnectController(MatrimonialDbContext context, ILogger<ConnectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Connect/Send/{userId}
        [HttpGet]
        public async Task<IActionResult> Send(long userId)
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (currentUserId == userId)
            {
                TempData["ErrorMessage"] = "You cannot send a connect request to yourself.";
                return RedirectToAction("Index", "Home");
            }

            // Check if user exists
            var targetUser = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            // Get primary photo
            var primaryPhoto = await _context.ProfilePhotos
                .Where(p => p.UserId == userId && p.IsPrimary)
                .FirstOrDefaultAsync();

            // Check if connect request already exists
            var existingRequest = await _context.UserInteractions
                .FirstOrDefaultAsync(ui => ui.FromUserId == currentUserId && 
                                          ui.ToUserId == userId && 
                                          ui.InteractionType == "connect");

            if (existingRequest != null)
            {
                ViewBag.ExistingRequest = existingRequest;
            }

            ViewBag.TargetUser = targetUser;
            ViewBag.TargetProfile = targetUser.UserProfile;
            ViewBag.PrimaryPhoto = primaryPhoto;
            return View();
        }

        // POST: Connect/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(long userId, string message)
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Json(new { success = false, message = "Please login to send connect requests." });
            }

            if (currentUserId == userId)
            {
                return Json(new { success = false, message = "You cannot send a connect request to yourself." });
            }

            // Check if user exists
            var targetUser = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (targetUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Check if connect request already exists
            var existingRequest = await _context.UserInteractions
                .FirstOrDefaultAsync(ui => ui.FromUserId == currentUserId && 
                                          ui.ToUserId == userId && 
                                          ui.InteractionType == "connect");

            if (existingRequest != null)
            {
                if (existingRequest.Status == "pending")
                {
                    return Json(new { success = false, message = "You have already sent a connect request to this user." });
                }
                else if (existingRequest.Status == "accepted")
                {
                    return Json(new { success = false, message = "You are already connected with this user." });
                }
                else
                {
                    // Update existing declined request
                    existingRequest.Message = message;
                    existingRequest.Status = "pending";
                    existingRequest.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Create new connect request
                var connectRequest = new UserInteraction
                {
                    FromUserId = currentUserId,
                    ToUserId = userId,
                    InteractionType = "connect",
                    Message = message,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserInteractions.Add(connectRequest);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Connect request sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending connect request from {FromUserId} to {ToUserId}", currentUserId, userId);
                return Json(new { success = false, message = "An error occurred while sending the connect request. Please try again." });
            }
        }

        // GET: Connect/Requests
        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get pending connect requests received
            var receivedRequests = await _context.UserInteractions
                .Where(ui => ui.ToUserId == currentUserId && 
                            ui.InteractionType == "connect" && 
                            ui.Status == "pending")
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();

            // Get pending connect requests sent
            var sentRequests = await _context.UserInteractions
                .Where(ui => ui.FromUserId == currentUserId && 
                            ui.InteractionType == "connect" && 
                            ui.Status == "pending")
                .Include(ui => ui.ToUser)
                .ThenInclude(u => u.UserProfile)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();

            // Get accepted connections
            var acceptedConnections = await _context.UserInteractions
                .Where(ui => (ui.FromUserId == currentUserId || ui.ToUserId == currentUserId) && 
                            ui.InteractionType == "connect" && 
                            ui.Status == "accepted")
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .Include(ui => ui.ToUser)
                .ThenInclude(u => u.UserProfile)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();

            // Load photos for all users
            var allUserIds = receivedRequests.Select(r => r.FromUserId)
                .Union(sentRequests.Select(s => s.ToUserId))
                .Union(acceptedConnections.SelectMany(a => new[] { a.FromUserId, a.ToUserId }))
                .Distinct()
                .ToList();

            var photos = await _context.ProfilePhotos
                .Where(p => allUserIds.Contains(p.UserId) && p.IsPrimary)
                .ToListAsync();

            var photosDict = photos.ToDictionary(p => p.UserId);

            ViewBag.ReceivedRequests = receivedRequests;
            ViewBag.SentRequests = sentRequests;
            ViewBag.AcceptedConnections = acceptedConnections;
            ViewBag.Photos = photosDict;

            return View();
        }

        // POST: Connect/Accept
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(long requestId)
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Json(new { success = false, message = "Please login to accept connect requests." });
            }

            var request = await _context.UserInteractions
                .FirstOrDefaultAsync(ui => ui.Id == requestId && 
                                          ui.ToUserId == currentUserId && 
                                          ui.InteractionType == "connect" && 
                                          ui.Status == "pending");

            if (request == null)
            {
                return Json(new { success = false, message = "Connect request not found or already processed." });
            }

            request.Status = "accepted";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Connect request accepted!" });
        }

        // POST: Connect/Decline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(long requestId)
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Json(new { success = false, message = "Please login to decline connect requests." });
            }

            var request = await _context.UserInteractions
                .FirstOrDefaultAsync(ui => ui.Id == requestId && 
                                          ui.ToUserId == currentUserId && 
                                          ui.InteractionType == "connect" && 
                                          ui.Status == "pending");

            if (request == null)
            {
                return Json(new { success = false, message = "Connect request not found or already processed." });
            }

            request.Status = "declined";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Connect request declined." });
        }

        // GET: Connect/Matches
        [HttpGet]
        public async Task<IActionResult> Matches()
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !long.TryParse(currentUserIdStr, out var currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get all accepted connections
            var acceptedConnections = await _context.UserInteractions
                .Where(ui => (ui.FromUserId == currentUserId || ui.ToUserId == currentUserId) && 
                            ui.InteractionType == "connect" && 
                            ui.Status == "accepted")
                .Include(ui => ui.FromUser)
                .ThenInclude(u => u.UserProfile)
                .Include(ui => ui.ToUser)
                .ThenInclude(u => u.UserProfile)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();

            // Get user IDs for all connected members
            var connectedUserIds = acceptedConnections
                .SelectMany(c => new[] { c.FromUserId, c.ToUserId })
                .Where(id => id != currentUserId)
                .Distinct()
                .ToList();

            // Load primary photos for all connected members
            var photos = await _context.ProfilePhotos
                .Where(p => connectedUserIds.Contains(p.UserId) && p.IsPrimary && p.Status != "deleted")
                .ToDictionaryAsync(p => p.UserId, p => p);

            ViewBag.AcceptedConnections = acceptedConnections;
            ViewBag.Photos = photos;
            ViewBag.CurrentUserId = currentUserId;

            return View();
        }
    }
}

