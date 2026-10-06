namespace HMS.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
}
