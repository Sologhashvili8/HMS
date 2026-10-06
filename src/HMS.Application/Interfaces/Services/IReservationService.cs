using HMS.Application.DTOs.Reservations;

namespace HMS.Application.Interfaces.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetAllAsync(int hotelId, int? roomId, Guid? guestId, DateOnly? date, bool? active);
    Task<ReservationDto> GetByIdAsync(int hotelId, int reservationId);
    Task<ReservationDto> CreateAsync(int hotelId, CreateReservationDto dto);
    Task<ReservationDto> UpdateAsync(int hotelId, int reservationId, UpdateReservationDto dto);
    Task DeleteAsync(int hotelId, int reservationId);
    Task<IReadOnlyList<MyReservationDto>> GetMyReservationsAsync();
    Task<ReservationDto> CreateForGuestAsync(int hotelId, CreateStaffReservationDto dto);
    Task<IReadOnlyList<StaffReservationDto>> GetStaffReservationsAsync(int hotelId);
}
