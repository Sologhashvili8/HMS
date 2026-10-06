using HMS.Application.Common;
using HMS.Application.DTOs.Hotels;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HotelDto>>>> GetAll(
        [FromQuery] string? country, [FromQuery] string? city, [FromQuery] byte? rating)
    {
        var hotels = await _hotelService.GetAllAsync(country, city, rating);
        return Ok(ApiResponse<IReadOnlyList<HotelDto>>.SuccessResponse(hotels));
    }

    [HttpGet("{hotelId}")]
    public async Task<ActionResult<ApiResponse<HotelDto>>> GetById(int hotelId)
    {
        var hotel = await _hotelService.GetByIdAsync(hotelId);
        return Ok(ApiResponse<HotelDto>.SuccessResponse(hotel));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<HotelDto>>> Create(CreateHotelDto dto)
    {
        var hotel = await _hotelService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { hotelId = hotel.Id }, ApiResponse<HotelDto>.SuccessResponse(hotel));
    }

    [HttpPut("{hotelId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<HotelDto>>> Update(int hotelId, UpdateHotelDto dto)
    {
        var hotel = await _hotelService.UpdateAsync(hotelId, dto);
        return Ok(ApiResponse<HotelDto>.SuccessResponse(hotel));
    }

    [HttpDelete("{hotelId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int hotelId)
    {
        await _hotelService.DeleteAsync(hotelId);
        return NoContent();
    }
}
