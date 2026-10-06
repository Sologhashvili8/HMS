using HMS.Application.DTOs.Rooms;

namespace HMS.Application.Interfaces.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetAllAsync(int hotelId, decimal? minPrice, decimal? maxPrice, DateOnly? checkIn, DateOnly? checkOut);
    Task<RoomDto> GetByIdAsync(int hotelId, int roomId);
    Task<RoomDto> CreateAsync(int hotelId, CreateRoomDto dto);
    Task<RoomDto> UpdateAsync(int hotelId, int roomId, UpdateRoomDto dto);
    Task DeleteAsync(int hotelId, int roomId);
}
