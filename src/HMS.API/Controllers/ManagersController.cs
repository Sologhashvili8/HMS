using HMS.Application.Common;
using HMS.Application.DTOs.Managers;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
public class ManagersController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagersController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    [HttpGet("api/managers/me")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<ActionResult<ApiResponse<ManagerDto>>> GetMe()
    {
        var manager = await _managerService.GetMyAsync();
        return Ok(ApiResponse<ManagerDto>.SuccessResponse(manager));
    }

    [HttpGet("api/hotels/{hotelId}/managers")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ManagerDto>>>> GetAll(int hotelId)
    {
        var managers = await _managerService.GetAllAsync(hotelId);
        return Ok(ApiResponse<IReadOnlyList<ManagerDto>>.SuccessResponse(managers));
    }

    [HttpPut("api/hotels/{hotelId}/manager")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<ManagerDto>>> Assign(int hotelId, AssignManagerDto dto)
    {
        var manager = await _managerService.AssignAsync(hotelId, dto.UserId);
        return Ok(ApiResponse<ManagerDto>.SuccessResponse(manager));
    }

    [HttpDelete("api/hotels/{hotelId}/manager")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Unassign(int hotelId)
    {
        await _managerService.UnassignAsync(hotelId);
        return NoContent();
    }
}
