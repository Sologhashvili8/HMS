using HMS.Application.DTOs.Guests;

namespace HMS.Application.Interfaces.Services;

public interface IGuestService
{
    Task<GuestDto> GetMyProfileAsync();
    Task<GuestDto> UpdateMyProfileAsync(UpdateGuestDto dto);
    Task DeleteMyProfileAsync();
}
