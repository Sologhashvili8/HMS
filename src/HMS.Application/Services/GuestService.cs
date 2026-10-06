using HMS.Application.DTOs.Guests;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Entities;
using Mapster;

namespace HMS.Application.Services;

public class GuestService : IGuestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityUserService _identityUserService;

    public GuestService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IIdentityUserService identityUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _identityUserService = identityUserService;
    }

    public async Task<GuestDto> GetMyProfileAsync()
    {
        var guest = await GetCurrentGuestAsync();
        return guest.Adapt<GuestDto>();
    }

    public async Task<GuestDto> UpdateMyProfileAsync(UpdateGuestDto dto)
    {
        var guest = await GetCurrentGuestAsync();

        guest.FirstName = dto.FirstName;
        guest.LastName = dto.LastName;
        guest.PhoneNumber = dto.PhoneNumber;

        _unitOfWork.Repository<Guest>().Update(guest);
        await _unitOfWork.SaveChangesAsync();

        return guest.Adapt<GuestDto>();
    }

    public async Task DeleteMyProfileAsync()
    {
        var guest = await GetCurrentGuestAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var blockingReservations = await _unitOfWork.Repository<Reservation>().FindAsync(r =>
            r.GuestId == guest.Id && r.CheckOutDate >= today);

        if (blockingReservations.Any())
            throw new BadRequestException("Cannot delete a guest profile with active or upcoming reservations.");

        await _identityUserService.DeleteUserAsync(guest.Id);
    }

    private async Task<Guest> GetCurrentGuestAsync()
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        return await _unitOfWork.Repository<Guest>().GetByIdAsync(userId)
            ?? throw new NotFoundException("Guest profile not found.");
    }
}
