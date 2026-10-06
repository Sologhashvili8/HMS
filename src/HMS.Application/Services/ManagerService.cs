using HMS.Application.DTOs.Managers;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;
using Mapster;

namespace HMS.Application.Services;

public class ManagerService : IManagerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityUserService _identityUserService;
    private readonly ICurrentUserService _currentUser;

    public ManagerService(IUnitOfWork unitOfWork, IIdentityUserService identityUserService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _identityUserService = identityUserService;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ManagerDto>> GetAllAsync(int hotelId)
    {
        await EnsureCanAccessHotelAsync(hotelId);

        var managers = await _unitOfWork.Repository<Manager>().FindAsync(m => m.HotelId == hotelId);
        return managers.Adapt<List<ManagerDto>>();
    }

    public async Task<ManagerDto> GetMyAsync()
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(userId)
            ?? throw new ForbiddenException("You are not assigned to any hotel.");

        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(manager.HotelId);

        var dto = manager.Adapt<ManagerDto>();
        dto.HotelName = hotel?.Name ?? string.Empty;
        return dto;
    }

    public async Task<ManagerDto> AssignAsync(int hotelId, Guid userId)
    {
        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(hotelId)
            ?? throw new NotFoundException($"Hotel with id {hotelId} was not found.");

        var hotelManagers = await _unitOfWork.Repository<Manager>().FindAsync(m => m.HotelId == hotelId);
        if (hotelManagers.Any())
            throw new ConflictException("This hotel already has a manager. Remove the current manager first.");

        var existingAssignment = await _unitOfWork.Repository<Manager>().GetByIdAsync(userId);
        if (existingAssignment is not null)
            throw new ConflictException("This user already manages another hotel.");

        var email = await _identityUserService.GetEmailAsync(userId)
            ?? throw new NotFoundException($"User with id {userId} was not found.");

        if (await _identityUserService.IsInRoleAsync(userId, Roles.Admin))
            throw new BadRequestException("An admin cannot be assigned as a hotel manager.");

        var guest = await _unitOfWork.Repository<Guest>().GetByIdAsync(userId)
            ?? throw new BadRequestException("This user has no profile and cannot be assigned as a manager.");

        var manager = new Manager
        {
            Id = userId,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            PersonalNumber = guest.PersonalNumber,
            Email = email,
            PhoneNumber = guest.PhoneNumber,
            HotelId = hotel.Id
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Repository<Manager>().AddAsync(manager);
            await _unitOfWork.SaveChangesAsync();
            await _identityUserService.AddToRoleAsync(userId, Roles.Manager);
        });

        var dto = manager.Adapt<ManagerDto>();
        dto.HotelName = hotel.Name;
        return dto;
    }

    public async Task UnassignAsync(int hotelId)
    {
        var managers = await _unitOfWork.Repository<Manager>().FindAsync(m => m.HotelId == hotelId);
        var manager = managers.FirstOrDefault()
            ?? throw new NotFoundException($"Hotel {hotelId} has no manager.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.Repository<Manager>().Remove(manager);
            await _unitOfWork.SaveChangesAsync();
            await _identityUserService.RemoveFromRoleAsync(manager.Id, Roles.Manager);
        });
    }

    private async Task EnsureCanAccessHotelAsync(int hotelId)
    {
        if (_currentUser.IsInRole(Roles.Admin))
            return;

        var managerId = _currentUser.UserId
            ?? throw new ForbiddenException("Not authenticated.");

        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(managerId);
        if (manager is null || manager.HotelId != hotelId)
            throw new ForbiddenException("You can only view your own hotel's managers.");
    }
}
