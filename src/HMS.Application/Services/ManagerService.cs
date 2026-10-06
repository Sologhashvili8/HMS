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

    public async Task<ManagerDto> GetByIdAsync(int hotelId, Guid managerId)
    {
        await EnsureCanAccessHotelAsync(hotelId);

        var manager = await GetManagerInHotelAsync(hotelId, managerId);
        return manager.Adapt<ManagerDto>();
    }

    public async Task<ManagerDto> CreateAsync(int hotelId, CreateManagerDto dto)
    {
        var hotel = await _unitOfWork.Repository<Hotel>().GetByIdAsync(hotelId)
            ?? throw new NotFoundException($"Hotel with id {hotelId} was not found.");

        var existingPersonalNumber = await _unitOfWork.Repository<Manager>().FindAsync(m => m.PersonalNumber == dto.PersonalNumber);
        if (existingPersonalNumber.Any())
            throw new ConflictException("A manager with this personal number already exists.");

        Manager? manager = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = await _identityUserService.CreateUserAsync(dto.Email, dto.Password, Roles.Manager);

            manager = new Manager
            {
                Id = userId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PersonalNumber = dto.PersonalNumber,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                HotelId = hotel.Id
            };

            await _unitOfWork.Repository<Manager>().AddAsync(manager);
            await _unitOfWork.SaveChangesAsync();
        });

        return manager!.Adapt<ManagerDto>();
    }

    public async Task<ManagerDto> UpdateAsync(int hotelId, Guid managerId, UpdateManagerDto dto)
    {
        var manager = await GetManagerInHotelAsync(hotelId, managerId);

        manager.FirstName = dto.FirstName;
        manager.LastName = dto.LastName;
        manager.PhoneNumber = dto.PhoneNumber;

        _unitOfWork.Repository<Manager>().Update(manager);
        await _unitOfWork.SaveChangesAsync();

        return manager.Adapt<ManagerDto>();
    }

    public async Task DeleteAsync(int hotelId, Guid managerId)
    {
        await GetManagerInHotelAsync(hotelId, managerId);

        var otherManagers = await _unitOfWork.Repository<Manager>().FindAsync(m => m.HotelId == hotelId && m.Id != managerId);
        if (!otherManagers.Any())
            throw new BadRequestException("Cannot delete the only manager of a hotel. Assign another manager first.");

        await _identityUserService.DeleteUserAsync(managerId);
    }

    private async Task<Manager> GetManagerInHotelAsync(int hotelId, Guid managerId)
    {
        var manager = await _unitOfWork.Repository<Manager>().GetByIdAsync(managerId);
        if (manager is null || manager.HotelId != hotelId)
            throw new NotFoundException($"Manager with id {managerId} was not found in hotel {hotelId}.");

        return manager;
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
