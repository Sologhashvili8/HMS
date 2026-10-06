using HMS.Application.DTOs.Amenities;

namespace HMS.Application.Interfaces.Services;

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityDto>> GetAllAsync();
}
