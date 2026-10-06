using HMS.Application.DTOs.Managers;

namespace HMS.Application.Interfaces.Services;

public interface IManagerService
{
    Task<IReadOnlyList<ManagerDto>> GetAllAsync(int hotelId);
    Task<ManagerDto> GetMyAsync();
    Task<ManagerDto> AssignAsync(int hotelId, Guid userId);
    Task UnassignAsync(int hotelId);
}
