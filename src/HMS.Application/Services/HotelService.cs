using HMS.Application.DTOs.Hotels;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;
using Mapster;

namespace HMS.Application.Services;

public class HotelService : IHotelService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityUserService _identityUserService;

    public HotelService(IUnitOfWork unitOfWork, IIdentityUserService identityUserService)
    {
        _unitOfWork = unitOfWork;
        _identityUserService = identityUserService;
    }

    public async Task<IReadOnlyList<HotelDto>> GetAllAsync(string? country, string? city, byte? rating)
    {
        var hotels = await _unitOfWork.Repository<Hotel>().FindAsync(h =>
            (string.IsNullOrEmpty(country) || h.Country == country) &&
            (string.IsNullOrEmpty(city) || h.City == city) &&
            (rating == null || h.Rating == rating));

        return hotels.Adapt<List<HotelDto>>();
    }

    public async Task<HotelDto> GetByIdAsync(int id)
    {
        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(id)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        return hotel.Adapt<HotelDto>();
    }

    public async Task<HotelDto> CreateAsync(CreateHotelDto dto)
    {
        var hotel = dto.Adapt<Hotel>();

        await _unitOfWork.Repository<Hotel>().AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();

        return hotel.Adapt<HotelDto>();
    }

    public async Task<HotelDto> UpdateAsync(int id, UpdateHotelDto dto)
    {
        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(id)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        hotel.Name = dto.Name;
        hotel.Address = dto.Address;
        hotel.Rating = dto.Rating;
        hotel.ImageUrl = dto.ImageUrl;

        _unitOfWork.Repository<Hotel>().Update(hotel);
        await _unitOfWork.SaveChangesAsync();

        return hotel.Adapt<HotelDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(id)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        var rooms = await _unitOfWork.Repository<Room>().FindAsync(r => r.HotelId == id);
        var roomIds = rooms.Select(r => r.Id).ToList();

        if (roomIds.Count > 0)
        {
            var reservationRooms = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr => roomIds.Contains(rr.RoomId));
            if (reservationRooms.Any())
                throw new BadRequestException("Cannot delete a hotel whose rooms have reservations.");
        }

        var managers = await _unitOfWork.Repository<Manager>().FindAsync(m => m.HotelId == id);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var manager in managers)
                _unitOfWork.Repository<Manager>().Remove(manager);

            foreach (var room in rooms)
                _unitOfWork.Repository<Room>().Remove(room);

            _unitOfWork.Repository<Hotel>().Remove(hotel);
            await _unitOfWork.SaveChangesAsync();

            foreach (var manager in managers)
                await _identityUserService.RemoveFromRoleAsync(manager.Id, Roles.Manager);
        });
    }
}
