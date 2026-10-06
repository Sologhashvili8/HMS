namespace HMS.Application.Interfaces;

public interface IIdentityUserService
{
    Task<Guid> CreateUserAsync(string email, string password, string role);
    Task DeleteUserAsync(Guid userId);
}
