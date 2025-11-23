using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class InboxController : Controller
    {
        private readonly MatrimonialDbContext _context;

        public InboxController(MatrimonialDbContext context)
        {
            _context = context;
        }

        // GET: Inbox
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get all conversations (matches) for this user
            var matches = await _context.Matches
                .Where(m => (m.User1Id == userId || m.User2Id == userId) && m.Status == "active")
                .Include(m => m.User1)
                .Include(m => m.User2)
                .Include(m => m.Messages.OrderByDescending(msg => msg.CreatedAt).Take(1))
                .ToListAsync();

            var conversations = new List<ConversationViewModel>();

            foreach (var match in matches)
            {
                var otherUser = match.User1Id == userId ? match.User2 : match.User1;
                var otherUserProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == otherUser.Id);

                // Get last message
                var lastMessage = await _context.Messages
                    .Where(m => m.MatchId == match.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                // Get unread count
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.MatchId == match.Id && m.ToUserId == userId && !m.IsRead);

                conversations.Add(new ConversationViewModel
                {
                    MatchId = match.Id,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUserProfile?.FullName ?? otherUser.Email,
                    OtherUserPhoto = await _context.ProfilePhotos
                        .Where(p => p.UserId == otherUser.Id && p.IsPrimary && p.Status == "approved")
                        .Select(p => p.ImageUrl)
                        .FirstOrDefaultAsync(),
                    LastMessage = lastMessage?.Content ?? "No messages yet",
                    LastMessageTime = lastMessage?.CreatedAt ?? match.CreatedAt,
                    UnreadCount = unreadCount,
                    IsLastMessageFromMe = lastMessage?.FromUserId == userId
                });
            }

            // Sort by last message time (most recent first)
            conversations = conversations.OrderByDescending(c => c.LastMessageTime).ToList();

            ViewBag.UnreadTotal = conversations.Sum(c => c.UnreadCount);
            return View(conversations);
        }

        // GET: Inbox/Conversation/{matchId} or ?userId={userId}
        public async Task<IActionResult> Conversation(long? matchId, long? userId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            Match? match = null;

            // If userId is provided, find or create a match
            if (userId.HasValue && userId.Value > 0)
            {
                var otherUserId = userId.Value;
                
                // Check if users are connected
                var isConnected = await _context.UserInteractions
                    .AnyAsync(ui => ui.InteractionType == "connect" && ui.Status == "accepted" &&
                                    ((ui.FromUserId == currentUserId && ui.ToUserId == otherUserId) ||
                                     (ui.FromUserId == otherUserId && ui.ToUserId == currentUserId)));

                if (!isConnected)
                {
                    TempData["ErrorMessage"] = "You need to be connected with this user to send messages.";
                    return RedirectToAction("View", "Profile", new { id = otherUserId });
                }

                // Find existing match
                match = await _context.Matches
                    .FirstOrDefaultAsync(m => 
                        ((m.User1Id == currentUserId && m.User2Id == otherUserId) ||
                         (m.User1Id == otherUserId && m.User2Id == currentUserId)) &&
                        m.Status == "active");

                // Create match if it doesn't exist
                if (match == null)
                {
                    match = new Match
                    {
                        User1Id = currentUserId,
                        User2Id = otherUserId,
                        Status = "active",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Matches.Add(match);
                    await _context.SaveChangesAsync();
                }
            }
            // If matchId is provided, use it
            else if (matchId.HasValue && matchId.Value > 0)
            {
                match = await _context.Matches
                    .Include(m => m.User1)
                    .Include(m => m.User2)
                    .FirstOrDefaultAsync(m => m.Id == matchId.Value && 
                                             (m.User1Id == currentUserId || m.User2Id == currentUserId) && 
                                             m.Status == "active");

                if (match == null)
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest("Either matchId or userId must be provided.");
            }

            // Load match with users if not already loaded
            if (match.User1 == null || match.User2 == null)
            {
                match = await _context.Matches
                    .Include(m => m.User1)
                    .Include(m => m.User2)
                    .FirstOrDefaultAsync(m => m.Id == match.Id);
            }

            var otherUser = match.User1Id == currentUserId ? match.User2 : match.User1;
            var otherUserProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == otherUser.Id);

            // Get all messages for this match
            var messages = await _context.Messages
                .Where(m => m.MatchId == match.Id)
                .Include(m => m.FromUser)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = messages.Where(m => m.ToUserId == currentUserId && !m.IsRead).ToList();
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            ViewBag.MatchId = match.Id;
            ViewBag.OtherUserId = otherUser.Id;
            ViewBag.OtherUserName = otherUserProfile?.FullName ?? otherUser.Email;
            ViewBag.OtherUserPhoto = await _context.ProfilePhotos
                .Where(p => p.UserId == otherUser.Id && p.IsPrimary && p.Status == "approved")
                .Select(p => p.ImageUrl)
                .FirstOrDefaultAsync();

            return View(messages);
        }

        // POST: Inbox/SendMessage
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage(long matchId, string content)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Json(new { success = false, message = "Please login to send messages." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            // Verify user is part of this match
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId && (m.User1Id == userId || m.User2Id == userId) && m.Status == "active");

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found." });
            }

            var toUserId = match.User1Id == userId ? match.User2Id : match.User1Id;

            var message = new Message
            {
                MatchId = matchId,
                FromUserId = userId,
                ToUserId = toUserId,
                Content = content.Trim(),
                ContentType = "text",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Message sent successfully.",
                messageId = message.Id,
                createdAt = message.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        // POST: Inbox/MarkAsRead
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAsRead(long messageId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Json(new { success = false, message = "Please login." });
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ToUserId == userId);

            if (message == null)
            {
                return Json(new { success = false, message = "Message not found." });
            }

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Message marked as read." });
        }
    }

    public class ConversationViewModel
    {
        public long MatchId { get; set; }
        public long OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserPhoto { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsLastMessageFromMe { get; set; }
    }
}



