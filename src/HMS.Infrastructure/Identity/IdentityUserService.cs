using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMS.Infrastructure.Identity;

public class IdentityUserService : IIdentityUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid> CreateUserAsync(string email, string password, string role)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            throw new ConflictException("A user with this email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role);

        return user.Id;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
            await _userManager.DeleteAsync(user);
    }

    public async Task<IReadOnlyList<UserSummary>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var result = new List<UserSummary>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserSummary(user.Id, user.Email ?? string.Empty, user.EmailConfirmed, roles.ToList()));
        }

        return result;
    }

    public async Task<string?> GetEmailAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.Email;
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task AddToRoleAsync(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"User with id {userId} was not found.");

        if (await _userManager.IsInRoleAsync(user, role))
            return;

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task RemoveFromRoleAsync(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !await _userManager.IsInRoleAsync(user, role))
            return;

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
