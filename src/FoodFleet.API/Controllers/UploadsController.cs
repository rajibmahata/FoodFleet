using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin image uploads — returns a CDN/storage URL for use in menu items and restaurant settings.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/uploads")]
[Produces("application/json")]
public class UploadsController : ApiControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>Upload an image file (JPEG, PNG, or WebP, max 5 MB). Returns the public URL.</summary>
    /// <response code="200">Image uploaded — <c>url</c> field contains the public URL.</response>
    /// <response code="400">File empty, too large, or wrong content type.</response>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Upload(IFormFile file, [FromServices] IFileStorageService storage, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { message = "File is empty." });
        if (file.Length > MaxFileSizeBytes) return BadRequest(new { message = "File exceeds 5 MB limit." });
        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, and WebP images are allowed." });

        using var stream = file.OpenReadStream();
        var url = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);
        return Ok(new { url });
    }
}
