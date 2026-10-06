using HMS.Application.DTOs.Rooms;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;
using Mapster;

namespace HMS.Application.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RoomService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(int hotelId, decimal? minPrice, decimal? maxPrice, DateOnly? checkIn, DateOnly? checkOut)
    {
        var rooms = await _unitOfWork.Repository<Room>().FindAsync(r =>
            r.HotelId == hotelId &&
            (minPrice == null || r.Price >= minPrice) &&
            (maxPrice == null || r.Price <= maxPrice));

        if (checkIn is not null && checkOut is not null)
        {
            var busyRoomIds = (await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr =>
                rr.Room.HotelId == hotelId &&
                rr.Reservation.CheckInDate < checkOut.Value &&
                rr.Reservation.CheckOutDate > checkIn.Value))
                .Select(rr => rr.RoomId)
                .ToHashSet();

            rooms = rooms.Where(r => !busyRoomIds.Contains(r.Id)).ToList();
        }

        var roomIds = rooms.Select(r => r.Id).ToList();
        var amenityNamesByRoom = await GetAmenityNamesByRoomAsync(roomIds);
        var photoUrlsByRoom = await GetPhotoUrlsByRoomAsync(roomIds);

        return rooms.Select(r => ToDto(r, amenityNamesByRoom, photoUrlsByRoom)).ToList();
    }

    public async Task<RoomDto> GetByIdAsync(int hotelId, int roomId)
    {
        var room = await GetRoomInHotelAsync(hotelId, roomId);
        var amenityNamesByRoom = await GetAmenityNamesByRoomAsync(new List<int> { room.Id });
        var photoUrlsByRoom = await GetPhotoUrlsByRoomAsync(new List<int> { room.Id });
        return ToDto(room, amenityNamesByRoom, photoUrlsByRoom);
    }

    public async Task<RoomDto> CreateAsync(int hotelId, CreateRoomDto dto)
    {
        await EnsureCanManageHotelAsync(hotelId);

        if (dto.Price <= 0)
            throw new BadRequestException("Price must be greater than 0.");

        if (dto.PhotoUrls.Distinct().Count() < 4)
            throw new BadRequestException("At least 4 photos are required.");

        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(hotelId)
            ?? throw new NotFoundException($"Hotel with id {hotelId} was not found.");

        var amenityIds = await ValidateAmenityIdsAsync(dto.AmenityIds);

        var room = new Room
        {
            Name = dto.Name,
            Price = dto.Price,
            Capacity = dto.Capacity,
            HotelId = hotel.Id,
            RoomAmenities = amenityIds.Select(id => new RoomAmenity { AmenityId = id }).ToList(),
            RoomPhotos = dto.PhotoUrls.Select(url => new RoomPhoto { Url = url }).ToList()
        };

        await _unitOfWork.Repository<Room>().AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var amenityNamesByRoom = await GetAmenityNamesByRoomAsync(new List<int> { room.Id });
        var photoUrlsByRoom = await GetPhotoUrlsByRoomAsync(new List<int> { room.Id });
        return ToDto(room, amenityNamesByRoom, photoUrlsByRoom);
    }

    public async Task<RoomDto> UpdateAsync(int hotelId, int roomId, UpdateRoomDto dto)
    {
        await EnsureCanManageHotelAsync(hotelId);

        if (dto.Price <= 0)
            throw new BadRequestException("Price must be greater than 0.");

        var room = await GetRoomInHotelAsync(hotelId, roomId);
        var amenityIds = await ValidateAmenityIdsAsync(dto.AmenityIds);

        room.Name = dto.Name;
        room.Price = dto.Price;
        room.Capacity = dto.Capacity;

        var existingLinks = await _unitOfWork.Repository<RoomAmenity>().FindAsync(ra => ra.RoomId == roomId);
        foreach (var link in existingLinks)
            _unitOfWork.Repository<RoomAmenity>().Remove(link);

        foreach (var amenityId in amenityIds)
            await _unitOfWork.Repository<RoomAmenity>().AddAsync(new RoomAmenity { RoomId = roomId, AmenityId = amenityId });

        var existingPhotos = await _unitOfWork.Repository<RoomPhoto>().FindAsync(p => p.RoomId == roomId);
        foreach (var photo in existingPhotos)
            _unitOfWork.Repository<RoomPhoto>().Remove(photo);

        foreach (var url in dto.PhotoUrls)
            await _unitOfWork.Repository<RoomPhoto>().AddAsync(new RoomPhoto { RoomId = roomId, Url = url });

        _unitOfWork.Repository<Room>().Update(room);
        await _unitOfWork.SaveChangesAsync();

        var amenityNamesByRoom = await GetAmenityNamesByRoomAsync(new List<int> { room.Id });
        var photoUrlsByRoom = await GetPhotoUrlsByRoomAsync(new List<int> { room.Id });
        return ToDto(room, amenityNamesByRoom, photoUrlsByRoom);
    }

    public async Task DeleteAsync(int hotelId, int roomId)
    {
        await EnsureCanManageHotelAsync(hotelId);

        var room = await GetRoomInHotelAsync(hotelId, roomId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var blockingReservations = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr =>
            rr.RoomId == roomId && rr.Reservation.CheckOutDate >= today);

        if (blockingReservations.Any())
            throw new BadRequestException("Cannot delete a room with active or upcoming reservations.");

        _unitOfWork.Repository<Room>().Remove(room);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Room> GetRoomInHotelAsync(int hotelId, int roomId)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(roomId);
        if (room is null || room.HotelId != hotelId)
            throw new NotFoundException($"Room with id {roomId} was not found in hotel {hotelId}.");

        return room;
    }

    private async Task EnsureCanManageHotelAsync(int hotelId)
    {
        if (_currentUser.IsInRole(Roles.Admin))
            return;

        var managerId = _currentUser.UserId
            ?? throw new ForbiddenException("Not authenticated.");

        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(managerId);
        if (manager is null || manager.HotelId != hotelId)
            throw new ForbiddenException("You can only manage your own hotel.");
    }

    private async Task<List<int>> ValidateAmenityIdsAsync(List<int> amenityIds)
    {
        var distinctIds = amenityIds.Distinct().ToList();
        if (distinctIds.Count == 0)
            return distinctIds;

        var amenities = await _unitOfWork.Repository<Amenity>().FindAsync(a => distinctIds.Contains(a.Id));
        if (amenities.Count != distinctIds.Count)
            throw new BadRequestException("One or more amenity ids do not exist.");

        return distinctIds;
    }

    private async Task<Dictionary<int, List<string>>> GetAmenityNamesByRoomAsync(List<int> roomIds)
    {
        if (roomIds.Count == 0)
            return new Dictionary<int, List<string>>();

        var links = await _unitOfWork.Repository<RoomAmenity>().FindAsync(ra => roomIds.Contains(ra.RoomId));

        var amenityIds = links.Select(l => l.AmenityId).Distinct().ToList();
        var amenities = await _unitOfWork.Repository<Amenity>().FindAsync(a => amenityIds.Contains(a.Id));
        var nameById = amenities.ToDictionary(a => a.Id, a => a.Name);

        return links
            .GroupBy(l => l.RoomId)
            .ToDictionary(g => g.Key, g => g.Select(l => nameById[l.AmenityId]).ToList());
    }

    private async Task<Dictionary<int, List<string>>> GetPhotoUrlsByRoomAsync(List<int> roomIds)
    {
        if (roomIds.Count == 0)
            return new Dictionary<int, List<string>>();

        var photos = await _unitOfWork.Repository<RoomPhoto>().FindAsync(p => roomIds.Contains(p.RoomId));

        return photos
            .GroupBy(p => p.RoomId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Url).ToList());
    }

    private static RoomDto ToDto(Room room, Dictionary<int, List<string>> amenityNamesByRoom, Dictionary<int, List<string>> photoUrlsByRoom)
    {
        var dto = room.Adapt<RoomDto>();
        dto.Amenities = amenityNamesByRoom.GetValueOrDefault(room.Id, new List<string>());
        dto.PhotoUrls = photoUrlsByRoom.GetValueOrDefault(room.Id, new List<string>());
        return dto;
    }
}
