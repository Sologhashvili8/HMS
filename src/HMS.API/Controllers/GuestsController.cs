using HMS.Application.Common;
using HMS.Application.DTOs.Guests;
using HMS.Application.DTOs.Reservations;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/guests")]
[Authorize(Roles = Roles.Guest)]
public class GuestsController : ControllerBase
{
    private readonly IGuestService _guestService;
    private readonly IReservationService _reservationService;

    public GuestsController(IGuestService guestService, IReservationService reservationService)
    {
        _guestService = guestService;
        _reservationService = reservationService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<GuestDto>>> GetMe()
    {
        var guest = await _guestService.GetMyProfileAsync();
        return Ok(ApiResponse<GuestDto>.SuccessResponse(guest));
    }

    [HttpGet("me/reservations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MyReservationDto>>>> GetMyReservations()
    {
        var reservations = await _reservationService.GetMyReservationsAsync();
        return Ok(ApiResponse<IReadOnlyList<MyReservationDto>>.SuccessResponse(reservations));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<GuestDto>>> UpdateMe(UpdateGuestDto dto)
    {
        var guest = await _guestService.UpdateMyProfileAsync(dto);
        return Ok(ApiResponse<GuestDto>.SuccessResponse(guest));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        await _guestService.DeleteMyProfileAsync();
        return NoContent();
    }
}
