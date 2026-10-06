using HMS.Application.Common;
using HMS.Application.DTOs.Rooms;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/hotels/{hotelId}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetAll(
        int hotelId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut)
    {
        var rooms = await _roomService.GetAllAsync(hotelId, minPrice, maxPrice, checkIn, checkOut);
        return Ok(ApiResponse<IReadOnlyList<RoomDto>>.SuccessResponse(rooms));
    }

    [HttpGet("{roomId}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> GetById(int hotelId, int roomId)
    {
        var room = await _roomService.GetByIdAsync(hotelId, roomId);
        return Ok(ApiResponse<RoomDto>.SuccessResponse(room));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Create(int hotelId, CreateRoomDto dto)
    {
        var room = await _roomService.CreateAsync(hotelId, dto);
        return CreatedAtAction(nameof(GetById), new { hotelId, roomId = room.Id }, ApiResponse<RoomDto>.SuccessResponse(room));
    }

    [HttpPut("{roomId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Update(int hotelId, int roomId, UpdateRoomDto dto)
    {
        var room = await _roomService.UpdateAsync(hotelId, roomId, dto);
        return Ok(ApiResponse<RoomDto>.SuccessResponse(room));
    }

    [HttpDelete("{roomId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> Delete(int hotelId, int roomId)
    {
        await _roomService.DeleteAsync(hotelId, roomId);
        return NoContent();
    }
}
