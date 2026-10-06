using HMS.Application.Common;
using HMS.Application.DTOs.Reservations;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/hotels/{hotelId}/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationDto>>>> GetAll(
        int hotelId, [FromQuery] int? roomId, [FromQuery] Guid? guestId, [FromQuery] DateOnly? date, [FromQuery] bool? active)
    {
        var reservations = await _reservationService.GetAllAsync(hotelId, roomId, guestId, date, active);
        return Ok(ApiResponse<IReadOnlyList<ReservationDto>>.SuccessResponse(reservations));
    }

    [HttpGet("{reservationId}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> GetById(int hotelId, int reservationId)
    {
        var reservation = await _reservationService.GetByIdAsync(hotelId, reservationId);
        return Ok(ApiResponse<ReservationDto>.SuccessResponse(reservation));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Guest)]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Create(int hotelId, CreateReservationDto dto)
    {
        var reservation = await _reservationService.CreateAsync(hotelId, dto);
        return CreatedAtAction(nameof(GetById), new { hotelId, reservationId = reservation.Id }, ApiResponse<ReservationDto>.SuccessResponse(reservation));
    }

    [HttpPut("{reservationId}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Update(int hotelId, int reservationId, UpdateReservationDto dto)
    {
        var reservation = await _reservationService.UpdateAsync(hotelId, reservationId, dto);
        return Ok(ApiResponse<ReservationDto>.SuccessResponse(reservation));
    }

    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> Delete(int hotelId, int reservationId)
    {
        await _reservationService.DeleteAsync(hotelId, reservationId);
        return NoContent();
    }
}
