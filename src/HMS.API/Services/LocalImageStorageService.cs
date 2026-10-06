using HMS.Application.Interfaces;

namespace HMS.API.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, "images", "uploads");
        Directory.CreateDirectory(directory);

        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        await using (var stream = File.Create(Path.Combine(directory, storedName)))
        {
            await content.CopyToAsync(stream);
        }

        return $"/images/uploads/{storedName}";
    }
}
