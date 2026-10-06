using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Infrastructure.Storage;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudinaryImageStorageService> _logger;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _folder;

    public CloudinaryImageStorageService(HttpClient httpClient, IConfiguration configuration, ILogger<CloudinaryImageStorageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cloudName = configuration["Cloudinary:CloudName"] ?? string.Empty;
        _apiKey = configuration["Cloudinary:ApiKey"] ?? string.Empty;
        _apiSecret = configuration["Cloudinary:ApiSecret"] ?? string.Empty;
        _folder = configuration["Cloudinary:Folder"] ?? "hms";
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign($"folder={_folder}&timestamp={timestamp}");

        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(_apiKey), "api_key");
        form.Add(new StringContent(timestamp), "timestamp");
        form.Add(new StringContent(_folder), "folder");
        form.Add(new StringContent(signature), "signature");

        var endpoint = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";
        using var response = await _httpClient.PostAsync(endpoint, form);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Cloudinary upload failed with {Status}: {Body}", (int)response.StatusCode, body);
            throw new BadRequestException("Image upload failed. Please try again.");
        }

        using var json = JsonDocument.Parse(body);
        var url = json.RootElement.GetProperty("secure_url").GetString()
            ?? throw new BadRequestException("Image upload failed. Please try again.");

        return url.Replace("/upload/", "/upload/f_auto,q_auto/");
    }

    private string Sign(string parameters)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(parameters + _apiSecret));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
