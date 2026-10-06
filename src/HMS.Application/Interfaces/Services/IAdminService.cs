using HMS.Application.DTOs.Admin;

namespace HMS.Application.Interfaces.Services;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync();
    Task<IReadOnlyList<AdminHotelDto>> GetHotelsAsync();
    Task DeleteUserAsync(Guid userId);
}
