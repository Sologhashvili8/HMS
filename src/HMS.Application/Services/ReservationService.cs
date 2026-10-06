using HMS.Application.DTOs.Reservations;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;

namespace HMS.Application.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IEncryptionService _encryptionService;
    private readonly IIdentityUserService _identityUserService;

    public ReservationService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IEncryptionService encryptionService, IIdentityUserService identityUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _encryptionService = encryptionService;
        _identityUserService = identityUserService;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetAllAsync(int hotelId, int? roomId, Guid? guestId, DateOnly? date, bool? active)
    {
        await EnsureCanAccessHotelAsync(hotelId);

        var effectiveGuestId = guestId;
        if (!_currentUser.IsInRole(Roles.Admin) && !_currentUser.IsInRole(Roles.Manager))
            effectiveGuestId = _currentUser.UserId;

        var reservationRooms = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr =>
            rr.Room.HotelId == hotelId &&
            (roomId == null || rr.RoomId == roomId));

        var reservationIds = reservationRooms.Select(rr => rr.ReservationId).Distinct().ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservations = await _unitOfWork.Repository<Reservation>().FindAsync(r =>
            reservationIds.Contains(r.Id) &&
            (effectiveGuestId == null || r.GuestId == effectiveGuestId) &&
            (date == null || (r.CheckInDate <= date && r.CheckOutDate > date)) &&
            (active == null || (active == true ? r.CheckOutDate >= today : r.CheckOutDate < today)));

        var roomsByReservation = reservationRooms
            .GroupBy(rr => rr.ReservationId)
            .ToDictionary(g => g.Key, g => g.Select(rr => rr.RoomId).ToList());

        return reservations
            .Select(r => ToDto(r, roomsByReservation.GetValueOrDefault(r.Id, new List<int>())))
            .ToList();
    }

    public async Task<ReservationDto> GetByIdAsync(int hotelId, int reservationId)
    {
        var (reservation, roomIds) = await GetReservationInHotelAsync(hotelId, reservationId);
        await EnsureCanViewReservationAsync(hotelId, reservation);
        return ToDto(reservation, roomIds);
    }

    public async Task<ReservationDto> CreateAsync(int hotelId, CreateReservationDto dto)
    {
        var guestId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        var requestedRoomIds = await PrepareRoomsAsync(hotelId, dto.CheckInDate, dto.CheckOutDate, dto.RoomIds);

        var reservation = new Reservation
        {
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            GuestId = guestId,
            ReservationRooms = requestedRoomIds.Select(id => new ReservationRoom { RoomId = id }).ToList()
        };

        // The reservation only becomes real once the (fake) payment has been captured,
        // so both writes happen in one transaction: no payment, no reserved rooms.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangesAsync();

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                EncryptedCardNumber = _encryptionService.Encrypt(dto.Payment.CardNumber),
                EncryptedPhoneNumber = _encryptionService.Encrypt(dto.Payment.PhoneNumber),
                EncryptedPersonalNumber = _encryptionService.Encrypt(dto.Payment.PersonalNumber),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Payment>().AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        });

        return ToDto(reservation, requestedRoomIds);
    }

    public async Task<ReservationDto> UpdateAsync(int hotelId, int reservationId, UpdateReservationDto dto)
    {
        var (reservation, roomIds) = await GetReservationInHotelAsync(hotelId, reservationId);
        await EnsureCanManageReservationAsync(hotelId, reservation);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.CheckInDate < today)
            throw new BadRequestException("Check-in date must be today or later.");
        if (dto.CheckOutDate <= dto.CheckInDate)
            throw new BadRequestException("Check-out date must be after check-in date.");

        var conflicting = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr =>
            roomIds.Contains(rr.RoomId) &&
            rr.ReservationId != reservationId &&
            rr.Reservation.CheckInDate < dto.CheckOutDate &&
            rr.Reservation.CheckOutDate > dto.CheckInDate);

        if (conflicting.Any())
            throw new BadRequestException("The new dates conflict with another reservation for one of the booked rooms.");

        reservation.CheckInDate = dto.CheckInDate;
        reservation.CheckOutDate = dto.CheckOutDate;

        _unitOfWork.Repository<Reservation>().Update(reservation);
        await _unitOfWork.SaveChangesAsync();

        return ToDto(reservation, roomIds);
    }

    public async Task DeleteAsync(int hotelId, int reservationId)
    {
        var (reservation, _) = await GetReservationInHotelAsync(hotelId, reservationId);
        await EnsureCanManageReservationAsync(hotelId, reservation);

        _unitOfWork.Repository<Reservation>().Remove(reservation);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MyReservationDto>> GetMyReservationsAsync()
    {
        var guestId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        var reservations = await _unitOfWork.Repository<Reservation>().FindAsync(r => r.GuestId == guestId);
        if (reservations.Count == 0)
            return new List<MyReservationDto>();

        var reservationIds = reservations.Select(r => r.Id).ToList();
        var reservationRooms = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr => reservationIds.Contains(rr.ReservationId));

        var roomIds = reservationRooms.Select(rr => rr.RoomId).Distinct().ToList();
        var rooms = await _unitOfWork.Repository<Room>().FindAsync(r => roomIds.Contains(r.Id));
        var roomsById = rooms.ToDictionary(r => r.Id);

        var hotelIds = rooms.Select(r => r.HotelId).Distinct().ToList();
        var hotels = await _unitOfWork.Repository<Hotel>().FindAsync(h => hotelIds.Contains(h.Id));
        var hotelsById = hotels.ToDictionary(h => h.Id);

        var roomsByReservation = reservationRooms
            .GroupBy(rr => rr.ReservationId)
            .ToDictionary(g => g.Key, g => g.Select(rr => rr.RoomId).ToList());

        var result = new List<MyReservationDto>();
        foreach (var reservation in reservations.OrderByDescending(r => r.CheckInDate))
        {
            var resRoomIds = roomsByReservation.GetValueOrDefault(reservation.Id, new List<int>());
            var resRooms = resRoomIds.Where(roomsById.ContainsKey).Select(id => roomsById[id]).ToList();
            if (resRooms.Count == 0)
                continue;

            var hotel = hotelsById.GetValueOrDefault(resRooms[0].HotelId);
            var nights = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

            result.Add(new MyReservationDto
            {
                Id = reservation.Id,
                HotelId = hotel?.Id ?? 0,
                HotelName = hotel?.Name ?? string.Empty,
                HotelCity = hotel?.City ?? string.Empty,
                HotelImageUrl = hotel?.ImageUrl ?? string.Empty,
                RoomNames = resRooms.Select(r => r.Name).ToList(),
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                Nights = nights,
                TotalPrice = resRooms.Sum(r => r.Price) * nights
            });
        }

        return result;
    }

    public async Task<ReservationDto> CreateForGuestAsync(int hotelId, CreateStaffReservationDto dto)
    {
        await EnsureStaffForHotelAsync(hotelId);

        var requestedRoomIds = await PrepareRoomsAsync(hotelId, dto.CheckInDate, dto.CheckOutDate, dto.RoomIds);

        Reservation? reservation = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var guestId = await ResolveGuestAsync(dto);

            reservation = new Reservation
            {
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                GuestId = guestId,
                ReservationRooms = requestedRoomIds.Select(id => new ReservationRoom { RoomId = id }).ToList()
            };

            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangesAsync();
        });

        return ToDto(reservation!, requestedRoomIds);
    }

    public async Task<IReadOnlyList<StaffReservationDto>> GetStaffReservationsAsync(int hotelId)
    {
        await EnsureStaffForHotelAsync(hotelId);

        var reservationRooms = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr => rr.Room.HotelId == hotelId);
        if (reservationRooms.Count == 0)
            return new List<StaffReservationDto>();

        var reservationIds = reservationRooms.Select(rr => rr.ReservationId).Distinct().ToList();
        var roomIds = reservationRooms.Select(rr => rr.RoomId).Distinct().ToList();

        var reservations = await _unitOfWork.Repository<Reservation>().FindAsync(r => reservationIds.Contains(r.Id));
        var guestIds = reservations.Select(r => r.GuestId).Distinct().ToList();
        var guests = (await _unitOfWork.Repository<Guest>().FindAsync(g => guestIds.Contains(g.Id))).ToDictionary(g => g.Id);
        var rooms = (await _unitOfWork.Repository<Room>().FindAsync(r => roomIds.Contains(r.Id))).ToDictionary(r => r.Id);

        var roomsByReservation = reservationRooms
            .GroupBy(rr => rr.ReservationId)
            .ToDictionary(g => g.Key, g => g.Select(rr => rr.RoomId).ToList());

        return reservations
            .OrderByDescending(r => r.CheckInDate)
            .Select(r =>
            {
                var resRooms = roomsByReservation.GetValueOrDefault(r.Id, new List<int>())
                    .Where(rooms.ContainsKey)
                    .Select(id => rooms[id])
                    .ToList();
                guests.TryGetValue(r.GuestId, out var guest);
                var nights = r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber;

                return new StaffReservationDto
                {
                    Id = r.Id,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    GuestId = r.GuestId,
                    GuestName = guest is null ? string.Empty : $"{guest.FirstName} {guest.LastName}",
                    GuestPhone = guest?.PhoneNumber ?? string.Empty,
                    GuestPersonalNumber = guest?.PersonalNumber ?? string.Empty,
                    RoomNames = resRooms.Select(x => x.Name).ToList(),
                    Nights = nights,
                    TotalPrice = resRooms.Sum(x => x.Price) * nights
                };
            })
            .ToList();
    }

    private async Task<List<int>> PrepareRoomsAsync(int hotelId, DateOnly checkIn, DateOnly checkOut, List<int>? roomIds)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (checkIn < today)
            throw new BadRequestException("Check-in date must be today or later.");
        if (checkOut <= checkIn)
            throw new BadRequestException("Check-out date must be after check-in date.");
        if (roomIds is null || roomIds.Count == 0)
            throw new BadRequestException("At least one room must be selected.");

        var requestedRoomIds = roomIds.Distinct().ToList();
        var rooms = await _unitOfWork.Repository<Room>().FindAsync(r => requestedRoomIds.Contains(r.Id) && r.HotelId == hotelId);
        if (rooms.Count != requestedRoomIds.Count)
            throw new BadRequestException("One or more rooms do not belong to this hotel.");

        var conflicting = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr =>
            requestedRoomIds.Contains(rr.RoomId) &&
            rr.Reservation.CheckInDate < checkOut &&
            rr.Reservation.CheckOutDate > checkIn);

        if (conflicting.Any())
            throw new BadRequestException("One or more selected rooms are not available for the given dates.");

        return requestedRoomIds;
    }

    private async Task<Guid> ResolveGuestAsync(CreateStaffReservationDto dto)
    {
        var existing = await _unitOfWork.Repository<Guest>().FindAsync(g => g.PersonalNumber == dto.PersonalNumber);
        var guest = existing.FirstOrDefault();
        if (guest is not null)
            return guest.Id;

        var phoneTaken = await _unitOfWork.Repository<Guest>().FindAsync(g => g.PhoneNumber == dto.PhoneNumber);
        if (phoneTaken.Any())
            throw new ConflictException("This phone number already belongs to another guest.");

        var userId = await _identityUserService.CreateUserAsync(
            $"walkin-{Guid.NewGuid():N}@walkin.local",
            $"Wk!{Guid.NewGuid():N}a1",
            Roles.Guest);

        await _unitOfWork.Repository<Guest>().AddAsync(new Guest
        {
            Id = userId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PersonalNumber = dto.PersonalNumber,
            PhoneNumber = dto.PhoneNumber
        });
        await _unitOfWork.SaveChangesAsync();

        return userId;
    }

    private async Task EnsureStaffForHotelAsync(int hotelId)
    {
        if (_currentUser.IsInRole(Roles.Admin))
            return;

        if (!_currentUser.IsInRole(Roles.Manager))
            throw new ForbiddenException("Only hotel staff can do this.");

        var managerId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");
        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(managerId);
        if (manager is null || manager.HotelId != hotelId)
            throw new ForbiddenException("You can only work with your own hotel.");
    }

    private async Task<(Reservation Reservation, List<int> RoomIds)> GetReservationInHotelAsync(int hotelId, int reservationId)
    {
        var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(reservationId)
            ?? throw new NotFoundException($"Reservation with id {reservationId} was not found.");

        var reservationRooms = await _unitOfWork.Repository<ReservationRoom>().FindAsync(rr => rr.ReservationId == reservationId);
        var roomIds = reservationRooms.Select(rr => rr.RoomId).ToList();

        var roomsInHotel = await _unitOfWork.Repository<Room>().FindAsync(r => roomIds.Contains(r.Id) && r.HotelId == hotelId);
        if (roomsInHotel.Count == 0)
            throw new NotFoundException($"Reservation with id {reservationId} was not found in hotel {hotelId}.");

        return (reservation, roomIds);
    }

    private async Task EnsureCanAccessHotelAsync(int hotelId)
    {
        if (!_currentUser.IsInRole(Roles.Manager) || _currentUser.IsInRole(Roles.Admin))
            return;

        var managerId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");
        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(managerId);
        if (manager is null || manager.HotelId != hotelId)
            throw new ForbiddenException("You can only view your own hotel's reservations.");
    }

    private async Task EnsureCanViewReservationAsync(int hotelId, Reservation reservation)
    {
        if (_currentUser.IsInRole(Roles.Admin))
            return;

        if (_currentUser.IsInRole(Roles.Manager))
        {
            await EnsureCanAccessHotelAsync(hotelId);
            return;
        }

        if (_currentUser.UserId != reservation.GuestId)
            throw new ForbiddenException("You can only view your own reservations.");
    }

    private async Task EnsureCanManageReservationAsync(int hotelId, Reservation reservation)
    {
        if (_currentUser.IsInRole(Roles.Admin))
            return;

        if (_currentUser.IsInRole(Roles.Manager))
        {
            await EnsureCanAccessHotelAsync(hotelId);
            return;
        }

        if (_currentUser.UserId != reservation.GuestId)
            throw new ForbiddenException("You can only manage your own reservations.");
    }

    private static ReservationDto ToDto(Reservation reservation, List<int> roomIds) => new()
    {
        Id = reservation.Id,
        CheckInDate = reservation.CheckInDate,
        CheckOutDate = reservation.CheckOutDate,
        GuestId = reservation.GuestId,
        RoomIds = roomIds
    };
}
