using HMS.Application.DTOs.Hotels;

namespace HMS.Application.Interfaces.Services;

public interface IHotelService
{
    Task<IReadOnlyList<HotelDto>> GetAllAsync(string? country, string? city, byte? rating);
    Task<HotelDto> GetByIdAsync(int id);
    Task<HotelDto> CreateAsync(CreateHotelDto dto);
    Task<HotelDto> UpdateAsync(int id, UpdateHotelDto dto);
    Task DeleteAsync(int id);
}
