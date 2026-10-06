using HMS.Application.DTOs.Admin;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;

namespace HMS.Application.Services;

public class AdminService : IAdminService
{
    private const string WalkInDomain = "@walkin.local";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityUserService _identityUserService;
    private readonly ICurrentUserService _currentUser;

    public AdminService(IUnitOfWork unitOfWork, IIdentityUserService identityUserService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _identityUserService = identityUserService;
        _currentUser = currentUser;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        if (_currentUser.UserId == userId)
            throw new BadRequestException("You cannot delete your own account.");

        var email = await _identityUserService.GetEmailAsync(userId)
            ?? throw new NotFoundException($"User with id {userId} was not found.");

        if (await _identityUserService.IsInRoleAsync(userId, Roles.Admin))
            throw new BadRequestException("An admin account cannot be deleted.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reservations = await _unitOfWork.Repository<Reservation>().FindAsync(r => r.GuestId == userId);
            foreach (var reservation in reservations)
                _unitOfWork.Repository<Reservation>().Remove(reservation);
            await _unitOfWork.SaveChangesAsync();

            var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(userId);
            if (manager is not null)
                _unitOfWork.Repository<Manager>().Remove(manager);

            var guest = await _unitOfWork.Repository<Guest>().GetByIdAsync(userId);
            if (guest is not null)
                _unitOfWork.Repository<Guest>().Remove(guest);
            await _unitOfWork.SaveChangesAsync();

            await _identityUserService.DeleteUserAsync(userId);
        });
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync()
    {
        var users = await _identityUserService.GetAllUsersAsync();
        var guests = (await _unitOfWork.Repository<Guest>().GetAllAsync()).ToDictionary(g => g.Id);
        var managers = (await _unitOfWork.Repository<Manager>().GetAllAsync()).ToDictionary(m => m.Id);
        var hotels = (await _unitOfWork.Repository<Hotel>().GetAllAsync()).ToDictionary(h => h.Id);

        var result = new List<AdminUserDto>();
        foreach (var user in users.Where(u => !u.Email.EndsWith(WalkInDomain, StringComparison.OrdinalIgnoreCase)))
        {
            var dto = new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Roles = user.Roles.ToList()
            };

            if (guests.TryGetValue(user.Id, out var guest))
            {
                dto.FirstName = guest.FirstName;
                dto.LastName = guest.LastName;
                dto.PhoneNumber = guest.PhoneNumber;
            }

            if (managers.TryGetValue(user.Id, out var manager))
            {
                dto.ManagedHotelId = manager.HotelId;
                dto.ManagedHotelName = hotels.GetValueOrDefault(manager.HotelId)?.Name;

                if (guest is null)
                {
                    dto.FirstName = manager.FirstName;
                    dto.LastName = manager.LastName;
                    dto.PhoneNumber = manager.PhoneNumber;
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<IReadOnlyList<AdminHotelDto>> GetHotelsAsync()
    {
        var hotels = await _unitOfWork.Repository<Hotel>().GetAllAsync();
        var managersByHotel = (await _unitOfWork.Repository<Manager>().GetAllAsync()).ToDictionary(m => m.HotelId);
        var rooms = await _unitOfWork.Repository<Room>().GetAllAsync();
        var roomCounts = rooms.GroupBy(r => r.HotelId).ToDictionary(g => g.Key, g => g.Count());

        return hotels
            .OrderBy(h => h.Name)
            .Select(h =>
            {
                managersByHotel.TryGetValue(h.Id, out var manager);
                return new AdminHotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Rating = h.Rating,
                    Country = h.Country,
                    City = h.City,
                    Address = h.Address,
                    ImageUrl = h.ImageUrl,
                    RoomsCount = roomCounts.GetValueOrDefault(h.Id, 0),
                    ManagerId = manager?.Id,
                    ManagerName = manager is null ? null : $"{manager.FirstName} {manager.LastName}",
                    ManagerEmail = manager?.Email
                };
            })
            .ToList();
    }
}
