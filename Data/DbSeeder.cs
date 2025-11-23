using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using testapp1.Models;

namespace testapp1.Data
{
    public static class DbSeeder
    {
        public static async Task SeedTestUserAsync(MatrimonialDbContext context)
        {
            // Check if test user already exists
            var testEmail = "test@example.com";
            if (await context.Users.AnyAsync(u => u.Email == testEmail))
            {
                return; // Test user already exists
            }

            // Create test user
            var user = new User
            {
                Email = testEmail,
                PasswordHash = HashPassword("Test123!"),
                Status = "active",
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Create user profile
            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = "Test User",
                Gender = "Male",
                DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 1, 1), DateTimeKind.Utc),
                MaritalStatus = "Never Married",
                Religion = "Hindu",
                City = "Mumbai",
                State = "Maharashtra",
                Country = "India",
                EducationLevel = "Bachelor's Degree",
                Occupation = "Software Engineer",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserProfiles.Add(profile);
            await context.SaveChangesAsync();

            Console.WriteLine($"Test user created successfully!");
            Console.WriteLine($"Email: {testEmail}");
            Console.WriteLine($"Password: Test123!");
        }

        public static async Task SeedKaliUserAndMessagesAsync(MatrimonialDbContext context)
        {
            // Check if Kali user already exists
            var kaliEmail = "kali@example.com";
            var kaliUser = await context.Users.FirstOrDefaultAsync(u => u.Email == kaliEmail);
            
            if (kaliUser == null)
            {
                // Create Kali user
                kaliUser = new User
                {
                    Email = kaliEmail,
                    PasswordHash = HashPassword("Kali123!"),
                    Status = "active",
                    Role = "user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(kaliUser);
                await context.SaveChangesAsync();

                // Create Kali's profile
                var kaliProfile = new UserProfile
                {
                    UserId = kaliUser.Id,
                    FullName = "Kali",
                    Gender = "Female",
                    DateOfBirth = DateTime.SpecifyKind(new DateTime(1995, 5, 15), DateTimeKind.Utc),
                    MaritalStatus = "Never Married",
                    Religion = "Hindu",
                    Caste = "Brahmin",
                    City = "Delhi",
                    State = "Delhi",
                    Country = "India",
                    EducationLevel = "Master's Degree",
                    Occupation = "Marketing Manager",
                    MotherTongue = "Hindi",
                    AboutMe = "I love traveling, reading books, and exploring new cuisines. Looking for someone who shares similar interests and values.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(kaliProfile);
                await context.SaveChangesAsync();

                Console.WriteLine($"Kali user created successfully!");
                Console.WriteLine($"Email: {kaliEmail}");
                Console.WriteLine($"Password: Kali123!");
            }

            // Get the first user (test user or any existing user) to create a match with Kali
            var otherUser = await context.Users
                .Where(u => u.Id != kaliUser.Id && u.Status == "active")
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            if (otherUser == null)
            {
                Console.WriteLine("No other user found to create a match with Kali.");
                return;
            }

            // Check if match already exists
            var existingMatch = await context.Matches
                .FirstOrDefaultAsync(m => 
                    (m.User1Id == kaliUser.Id && m.User2Id == otherUser.Id) ||
                    (m.User1Id == otherUser.Id && m.User2Id == kaliUser.Id));

            Match match;
            if (existingMatch == null)
            {
                // Create match between Kali and the other user
                match = new Match
                {
                    User1Id = Math.Min(kaliUser.Id, otherUser.Id),
                    User2Id = Math.Max(kaliUser.Id, otherUser.Id),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow.AddDays(-5) // Match created 5 days ago
                };

                context.Matches.Add(match);
                await context.SaveChangesAsync();
                Console.WriteLine($"Match created between Kali and User {otherUser.Id}");
            }
            else
            {
                match = existingMatch;
                Console.WriteLine($"Match already exists between Kali and User {otherUser.Id}");
            }

            // Check if messages already exist for this match
            var existingMessages = await context.Messages
                .Where(m => m.MatchId == match.Id)
                .CountAsync();

            if (existingMessages > 0)
            {
                Console.WriteLine($"Messages already exist for this match. Count: {existingMessages}");
                return;
            }

            // Add sample messages
            var messages = new List<Message>
            {
                // Kali sends first message
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = kaliUser.Id,
                    ToUserId = otherUser.Id,
                    Content = "Hi! I saw your profile and I'm really interested in getting to know you better. Would you like to chat?",
                    ContentType = "text",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(10)
                },
                // Other user replies
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = otherUser.Id,
                    ToUserId = kaliUser.Id,
                    Content = "Hello Kali! Thank you for reaching out. I'd love to chat with you too. How are you doing today?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(14)
                },
                // Kali responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = kaliUser.Id,
                    ToUserId = otherUser.Id,
                    Content = "I'm doing great, thank you! I noticed we have some common interests. Do you enjoy traveling?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(9)
                },
                // Other user responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = otherUser.Id,
                    ToUserId = kaliUser.Id,
                    Content = "Yes, I love traveling! I've been to several places in India. What about you? Any favorite destinations?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(16)
                },
                // Kali responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = kaliUser.Id,
                    ToUserId = otherUser.Id,
                    Content = "That's wonderful! I've been to Goa, Kerala, and Rajasthan. I'm planning a trip to Himachal Pradesh next month. Would you like to share some travel tips?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(11)
                },
                // Other user responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = otherUser.Id,
                    ToUserId = kaliUser.Id,
                    Content = "Himachal is beautiful! I've been there a couple of times. I'd be happy to share some recommendations. Have you thought about what you're looking for in a partner?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(18)
                },
                // Kali responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = kaliUser.Id,
                    ToUserId = otherUser.Id,
                    Content = "I'm looking for someone who is understanding, supportive, and shares similar values. Someone who appreciates family and wants to build a life together. What about you?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(10)
                },
                // Other user responds
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = otherUser.Id,
                    ToUserId = kaliUser.Id,
                    Content = "That sounds wonderful. I value the same things. I think we have a lot in common. Would you like to meet for coffee sometime?",
                    ContentType = "text",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(15)
                },
                // Kali responds (most recent, unread)
                new Message
                {
                    MatchId = match.Id,
                    FromUserId = kaliUser.Id,
                    ToUserId = otherUser.Id,
                    Content = "I'd love to! That sounds like a great idea. When would be convenient for you? I'm free this weekend.",
                    ContentType = "text",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };

            context.Messages.AddRange(messages);
            await context.SaveChangesAsync();

            Console.WriteLine($"Added {messages.Count} messages between Kali and User {otherUser.Id}");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

