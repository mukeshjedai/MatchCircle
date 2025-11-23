using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using testapp1.Data;
using testapp1.Models;

namespace testapp1.Controllers
{
    public class PhotoAlbumController : Controller
    {
        private readonly MatrimonialDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const int MaxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public PhotoAlbumController(MatrimonialDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: PhotoAlbum
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var photos = await _context.ProfilePhotos
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            // Auto-set profile picture if there are photos but none is set as primary
            if (photos.Any() && !photos.Any(p => p.IsPrimary))
            {
                // Set the first approved photo as primary, or first photo if none approved
                var photoToSet = photos.FirstOrDefault(p => p.Status == "approved") ?? photos.First();
                if (photoToSet != null)
                {
                    photoToSet.IsPrimary = true;
                    await _context.SaveChangesAsync();
                    // Reload photos to reflect the change
                    photos = await _context.ProfilePhotos
                        .Where(p => p.UserId == userId)
                        .OrderBy(p => p.SortOrder)
                        .ThenBy(p => p.CreatedAt)
                        .ToListAsync();
                }
            }

            ViewBag.MaxPhotos = 10; // Maximum number of photos allowed
            return View(photos);
        }

        // POST: PhotoAlbum/Upload
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
        public async Task<IActionResult> Upload(IFormFile[] files)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                {
                    return Json(new { success = false, message = "Please login to upload photos." });
                }

                if (files == null || files.Length == 0)
                {
                    return Json(new { success = false, message = "Please select at least one photo to upload." });
                }

                // Check current photo count
                var currentPhotoCount = await _context.ProfilePhotos
                    .CountAsync(p => p.UserId == userId && p.Status != "deleted");
                
                const int maxPhotos = 10;
                if (currentPhotoCount + files.Length > maxPhotos)
                {
                    return Json(new { success = false, message = $"You can upload a maximum of {maxPhotos} photos. You currently have {currentPhotoCount} photos." });
                }

                var uploadedPhotos = new List<ProfilePhoto>();
                var errors = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        // Validate file
                        var validationResult = ValidateFile(file);
                        if (!validationResult.IsValid)
                        {
                            errors.Add($"{file.FileName}: {validationResult.ErrorMessage}");
                            continue;
                        }

                        // Generate unique filename - sanitize filename
                        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? ".jpg";
                        if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension))
                        {
                            errors.Add($"{file.FileName}: Invalid file extension.");
                            continue;
                        }
                        
                        var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "photos");
                        
                        // Ensure directory exists
                        try
                        {
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }
                        }
                        catch (Exception dirEx)
                        {
                            errors.Add($"{file.FileName}: Failed to create upload directory - {dirEx.Message}");
                            continue;
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Save file with error handling
                        try
                        {
                            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await file.CopyToAsync(stream);
                                await stream.FlushAsync();
                            }
                        }
                        catch (Exception saveEx)
                        {
                            errors.Add($"{file.FileName}: Failed to save file - {saveEx.Message}");
                            continue;
                        }

                        // Get current max sort order
                        var existingPhotos = await _context.ProfilePhotos
                            .Where(p => p.UserId == userId)
                            .ToListAsync();
                        var maxSortOrder = existingPhotos.Any() 
                            ? existingPhotos.Max(p => p.SortOrder) 
                            : 0;

                        // Check if this is the first photo or if no primary photo exists (set as primary)
                        var hasPrimaryPhoto = existingPhotos.Any(p => p.IsPrimary);
                        var isFirstPhoto = currentPhotoCount == 0;
                        var shouldBePrimary = isFirstPhoto || !hasPrimaryPhoto;

                        // Create database record
                        var photo = new ProfilePhoto
                        {
                            UserId = userId,
                            ImageUrl = $"/uploads/photos/{fileName}",
                            IsPrimary = shouldBePrimary,
                            Status = "pending", // Photos need approval
                            SortOrder = maxSortOrder + 1,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        // If setting this as primary, unset others
                        if (shouldBePrimary)
                        {
                            foreach (var existingPhoto in existingPhotos.Where(p => p.IsPrimary))
                            {
                                existingPhoto.IsPrimary = false;
                            }
                        }

                        _context.ProfilePhotos.Add(photo);
                        uploadedPhotos.Add(photo);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file.FileName}: Upload failed - {ex.Message}");
                    }
                }

                if (uploadedPhotos.Count > 0)
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        // Rollback file saves if database save fails
                        foreach (var photo in uploadedPhotos)
                        {
                            try
                            {
                                var fileToDelete = Path.Combine(_environment.WebRootPath, photo.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(fileToDelete))
                                {
                                    System.IO.File.Delete(fileToDelete);
                                }
                            }
                            catch { }
                        }
                        return Json(new { success = false, message = $"Database error: {dbEx.Message}" });
                    }
                }

                if (errors.Count > 0 && uploadedPhotos.Count == 0)
                {
                    return Json(new { success = false, message = string.Join(" | ", errors) });
                }

                var message = uploadedPhotos.Count > 0 
                    ? $"Successfully uploaded {uploadedPhotos.Count} photo(s)." 
                    : "No photos were uploaded.";
                
                if (errors.Count > 0)
                {
                    message += " | Errors: " + string.Join(" | ", errors);
                }

                return Json(new { 
                    success = uploadedPhotos.Count > 0, 
                    message = message,
                    photos = uploadedPhotos.Select(p => new {
                        id = p.Id,
                        imageUrl = p.ImageUrl,
                        isPrimary = p.IsPrimary,
                        status = p.Status
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }

        // POST: PhotoAlbum/Delete
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Json(new { success = false, message = "Please login to delete photos." });
            }

            var photo = await _context.ProfilePhotos
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found." });
            }

            // Delete physical file
            if (!string.IsNullOrEmpty(photo.ImageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, photo.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        // Log error but continue with database deletion
                    }
                }
            }

            // If this was the primary photo, set another one as primary
            if (photo.IsPrimary)
            {
                // Try to set an approved photo first, otherwise any photo
                var nextPhoto = await _context.ProfilePhotos
                    .Where(p => p.UserId == userId && p.Id != id && p.Status != "deleted")
                    .OrderBy(p => p.Status == "approved" ? 0 : 1) // Prefer approved photos
                    .ThenBy(p => p.SortOrder)
                    .ThenBy(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextPhoto != null)
                {
                    nextPhoto.IsPrimary = true;
                }
            }

            // Soft delete or hard delete
            _context.ProfilePhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Photo deleted successfully." });
        }

        // POST: PhotoAlbum/SetPrimary
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetPrimary(long id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Json(new { success = false, message = "Please login to set primary photo." });
            }

            var photo = await _context.ProfilePhotos
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found." });
            }

            // Allow setting any photo as primary (not just approved ones)
            // Users can set pending photos as primary too

            // Unset all other primary photos
            var otherPrimaryPhotos = await _context.ProfilePhotos
                .Where(p => p.UserId == userId && p.IsPrimary && p.Id != id)
                .ToListAsync();

            foreach (var otherPhoto in otherPrimaryPhotos)
            {
                otherPhoto.IsPrimary = false;
            }

            // Set this photo as primary
            photo.IsPrimary = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Profile picture updated successfully." });
        }

        // POST: PhotoAlbum/Reorder
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Reorder(long[] photoIds)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Json(new { success = false, message = "Please login to reorder photos." });
            }

            if (photoIds == null || photoIds.Length == 0)
            {
                return Json(new { success = false, message = "No photos to reorder." });
            }

            var photos = await _context.ProfilePhotos
                .Where(p => p.UserId == userId && photoIds.Contains(p.Id))
                .ToListAsync();

            if (photos.Count != photoIds.Length)
            {
                return Json(new { success = false, message = "Some photos were not found." });
            }

            // Update sort order based on array order
            for (int i = 0; i < photoIds.Length; i++)
            {
                var photo = photos.FirstOrDefault(p => p.Id == photoIds[i]);
                if (photo != null)
                {
                    photo.SortOrder = i + 1;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Photo order updated successfully." });
        }

        private (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null)
            {
                return (false, "File is null.");
            }

            if (file.Length == 0)
            {
                return (false, "File is empty.");
            }

            if (file.Length > MaxFileSize)
            {
                return (false, $"File size exceeds {MaxFileSize / (1024 * 1024)}MB limit.");
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return (false, $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
            }

            // Check content type (more lenient - some browsers may not send correct content type)
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "application/octet-stream" };
            var contentType = file.ContentType?.ToLowerInvariant() ?? "";
            
            // If content type is not in allowed list but extension is valid, still allow (browser issue)
            if (!string.IsNullOrEmpty(contentType) && !allowedContentTypes.Contains(contentType) && contentType != "application/octet-stream")
            {
                // Only warn if it's clearly not an image
                if (!contentType.StartsWith("image/"))
                {
                    return (false, "File does not appear to be an image.");
                }
            }

            return (true, string.Empty);
        }
    }
}

