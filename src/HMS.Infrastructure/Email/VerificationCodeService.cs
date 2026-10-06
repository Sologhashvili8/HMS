using System.Security.Cryptography;
using HMS.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HMS.Infrastructure.Email;

public class VerificationCodeService : IVerificationCodeService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;

    public VerificationCodeService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Generate(string purpose, string email)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _cache.Set(Key(purpose, email), new Entry(code), Lifetime);
        return code;
    }

    public bool Verify(string purpose, string email, string code)
    {
        var key = Key(purpose, email);
        if (!_cache.TryGetValue<Entry>(key, out var entry) || entry is null)
            return false;

        if (entry.Attempts >= MaxAttempts)
        {
            _cache.Remove(key);
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(entry.Code),
                System.Text.Encoding.UTF8.GetBytes(code ?? string.Empty)))
        {
            entry.Attempts++;
            return false;
        }

        _cache.Remove(key);
        return true;
    }

    private static string Key(string purpose, string email) => $"{purpose}:{email.Trim().ToLowerInvariant()}";

    private sealed class Entry
    {
        public Entry(string code) => Code = code;
        public string Code { get; }
        public int Attempts { get; set; }
    }
}
