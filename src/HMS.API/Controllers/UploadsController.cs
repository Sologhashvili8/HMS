using HMS.Application.Common;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

public record UploadResultDto(string Url);

[ApiController]
[Route("api/uploads")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
public class UploadsController : ControllerBase
{
    private const long MaxFileSize = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".avif"
    };

    private readonly IImageStorageService _imageStorage;

    public UploadsController(IImageStorageService imageStorage)
    {
        _imageStorage = imageStorage;
    }

    [HttpPost("images")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadResultDto>>> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("File is empty.");

        if (file.Length > MaxFileSize)
            throw new BadRequestException("File is too large. Maximum size is 8 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new BadRequestException("Only jpg, png, webp and avif images are allowed.");

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Only image files are allowed.");

        await using var stream = file.OpenReadStream();
        var url = await _imageStorage.UploadAsync(stream, file.FileName, file.ContentType);

        return Ok(ApiResponse<UploadResultDto>.SuccessResponse(new UploadResultDto(url)));
    }
}
