using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using testapp1.Data;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace testapp1.Middleware
{
    public class SessionRestoreMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionRestoreMiddleware> _logger;

        public SessionRestoreMiddleware(RequestDelegate next, ILogger<SessionRestoreMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, MatrimonialDbContext dbContext)
        {
            // Only process if session doesn't have UserId
            // Check both session and cookie authentication
            var hasSessionUserId = context.Session.Keys.Contains("UserId");
            var isCookieAuthenticated = context.User?.Identity?.IsAuthenticated == true;

            if (!hasSessionUserId && !isCookieAuthenticated)
            {
                // Try to restore from persistent cookie
                var authToken = context.Request.Cookies[".MatchCircle.AuthToken"];
                var userIdStr = context.Request.Cookies[".MatchCircle.UserId"];

                if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out var userId))
                    {
                        try
                        {
                            // Verify token from database
                            var tokenHash = HashToken(authToken);
                            var session = await dbContext.UserLoginSessions
                                .FirstOrDefaultAsync(s => s.UserId == userId && 
                                    s.AuthTokenHash == tokenHash && 
                                    s.ExpiresAt > DateTime.UtcNow);

                            if (session != null)
                            {
                                // Restore session
                                context.Session.SetString("UserId", userId.ToString());
                                context.Session.SetString("AuthToken", authToken);
                                
                                // Update last activity (only once per hour to avoid too many DB writes)
                                var user = await dbContext.Users.FindAsync(userId);
                                if (user != null && (user.LastLoginAt == null || 
                                    (DateTime.UtcNow - user.LastLoginAt.Value).TotalHours > 1))
                                {
                                    user.LastLoginAt = DateTime.UtcNow;
                                    await dbContext.SaveChangesAsync();
                                }

                                _logger.LogInformation($"Session restored for user {userId}");
                            }
                            else
                            {
                                // Invalid token, clear cookies
                                context.Response.Cookies.Delete(".MatchCircle.AuthToken");
                                context.Response.Cookies.Delete(".MatchCircle.UserId");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error restoring session from cookie");
                        }
                    }
                }
            }
            else if (hasSessionUserId && !isCookieAuthenticated)
            {
                // Session exists but cookie auth doesn't - restore cookie auth
                var userIdStr = context.Session.GetString("UserId");
                var authToken = context.Session.GetString("AuthToken");
                
                if (!string.IsNullOrEmpty(userIdStr) && !string.IsNullOrEmpty(authToken))
                {
                    if (long.TryParse(userIdStr, out var userId))
                    {
                        try
                        {
                            var user = await dbContext.Users.FindAsync(userId);
                            if (user != null)
                            {
                                // Create claims and sign in
                                var claims = new List<System.Security.Claims.Claim>
                                {
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userIdStr),
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Email)
                                };

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                                    AllowRefresh = true
                                };
                                await context.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);
                                
                                // Set persistent cookies
                                SetPersistentCookies(context, userIdStr, authToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error restoring cookie authentication");
                        }
                    }
                }
            }

            await _next(context);
        }

        private static void SetPersistentCookies(HttpContext context, string userId, string authToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                IsEssential = true
            };

            context.Response.Cookies.Append(".MatchCircle.UserId", userId, cookieOptions);
            context.Response.Cookies.Append(".MatchCircle.AuthToken", authToken, cookieOptions);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public static class SessionRestoreMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionRestore(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionRestoreMiddleware>();
        }
    }
}

