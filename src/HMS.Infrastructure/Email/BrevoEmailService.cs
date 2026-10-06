using System.Net.Http.Json;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Infrastructure.Email;

public class BrevoEmailService : IEmailService
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _httpClient;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public BrevoEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Brevo:ApiKey"] ?? string.Empty;
        _senderEmail = configuration["Brevo:SenderEmail"] ?? string.Empty;
        _senderName = configuration["Brevo:SenderName"] ?? "BookNRest";
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_senderEmail))
        {
            _logger.LogError("Brevo is not configured. Set Brevo__ApiKey and Brevo__SenderEmail.");
            throw new BadRequestException("Email service is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("api-key", _apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { name = _senderName, email = _senderEmail },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = htmlBody
        });

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Brevo send failed with {Status}: {Body}", (int)response.StatusCode, body);
            throw new BadRequestException("Could not send the email. Please try again later.");
        }
    }
}
