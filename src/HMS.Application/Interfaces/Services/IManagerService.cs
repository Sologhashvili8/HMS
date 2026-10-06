using HMS.Application.DTOs.Managers;

namespace HMS.Application.Interfaces.Services;

public interface IManagerService
{
    Task<IReadOnlyList<ManagerDto>> GetAllAsync(int hotelId);
    Task<ManagerDto> GetByIdAsync(int hotelId, Guid managerId);
    Task<ManagerDto> CreateAsync(int hotelId, CreateManagerDto dto);
    Task<ManagerDto> UpdateAsync(int hotelId, Guid managerId, UpdateManagerDto dto);
    Task DeleteAsync(int hotelId, Guid managerId);
}
