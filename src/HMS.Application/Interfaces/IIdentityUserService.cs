namespace HMS.Application.Interfaces;

public record UserSummary(Guid Id, string Email, bool EmailConfirmed, IReadOnlyList<string> Roles);

public interface IIdentityUserService
{
    Task<Guid> CreateUserAsync(string email, string password, string role);
    Task DeleteUserAsync(Guid userId);
    Task<IReadOnlyList<UserSummary>> GetAllUsersAsync();
    Task<string?> GetEmailAsync(Guid userId);
    Task<bool> IsInRoleAsync(Guid userId, string role);
    Task AddToRoleAsync(Guid userId, string role);
    Task RemoveFromRoleAsync(Guid userId, string role);
}
