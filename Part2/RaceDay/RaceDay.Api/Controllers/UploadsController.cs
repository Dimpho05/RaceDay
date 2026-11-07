using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[SessionAuthorize]
public class UploadsController : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UploadsController(RaceDayDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Uploads (or replaces) the banner image of an event. Only the Organiser who owns the event may do this.
    /// The image is stored in Azure Blob Storage and its URL is saved on the event.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="file">A JPG, PNG or WEBP image, 5 MB or smaller.</param>
    [HttpPost("events/{id:int}/banner")]
    [SessionAuthorize("Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile? file)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Ownership check: only the Organiser who created the event may change its banner
        if (ev.OrganiserID != HttpContext.Session.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only upload banners for your own events." });
        }

        var problem = await ValidateImageAsync(file);
        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        var oldUrl = ev.BannerUrl;

        using (var stream = file!.OpenReadStream())
        {
            ev.BannerUrl = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType, "banners");
        }

        await _context.SaveChangesAsync();

        // Remove the previous banner so old files do not pile up in storage
        await _blobStorage.DeleteAsync(oldUrl);

        var eventType = await _context.EventTypes.FindAsync(ev.EventTypeID);

        return Ok(new EventResponse
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            BannerUrl = ev.BannerUrl,
            EventTypeID = ev.EventTypeID,
            EventTypeName = eventType?.TypeName ?? string.Empty,
            OrganiserID = ev.OrganiserID,
            CreateAt = ev.CreateAt
        });
    }

    /// <summary>
    /// Uploads (or replaces) the logged-in user's profile picture.
    /// The image is stored in Azure Blob Storage and its URL is saved on the user.
    /// </summary>
    /// <param name="file">A JPG, PNG or WEBP image, 5 MB or smaller.</param>
    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file)
    {
        var userId = HttpContext.Session.GetUserId();
        var user = await _context.Users.FindAsync(userId!.Value);
        if (user == null)
        {
            return Unauthorized(new { message = "Your account no longer exists." });
        }

        var problem = await ValidateImageAsync(file);
        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        var oldUrl = user.ProfilePictureUrl;

        using (var stream = file!.OpenReadStream())
        {
            user.ProfilePictureUrl = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType, "profiles");
        }

        await _context.SaveChangesAsync();

        await _blobStorage.DeleteAsync(oldUrl);

        return Ok(new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    // Returns a friendly message if the file is not an acceptable image, otherwise null
    private static async Task<string?> ValidateImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) ||
            !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return "Only JPG, PNG or WEBP images are allowed.";
        }

        // Check the first bytes of the file too, so a renamed non-image is rejected
        var header = new byte[12];
        using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < header.Length)
            {
                return "That file is not a valid image.";
            }
        }

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                     header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return isJpeg || isPng || isWebp ? null : "That file is not a valid image.";
    }
}