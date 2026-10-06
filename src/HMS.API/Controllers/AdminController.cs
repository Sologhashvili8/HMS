using HMS.Application.Common;
using HMS.Application.DTOs.Admin;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserDto>>>> GetUsers()
    {
        var users = await _adminService.GetUsersAsync();
        return Ok(ApiResponse<IReadOnlyList<AdminUserDto>>.SuccessResponse(users));
    }

    [HttpGet("hotels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminHotelDto>>>> GetHotels()
    {
        var hotels = await _adminService.GetHotelsAsync();
        return Ok(ApiResponse<IReadOnlyList<AdminHotelDto>>.SuccessResponse(hotels));
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        await _adminService.DeleteUserAsync(userId);
        return NoContent();
    }
}
