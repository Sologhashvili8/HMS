using HMS.Application.Common;
using HMS.Application.DTOs.Managers;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/hotels/{hotelId}/managers")]
public class ManagersController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagersController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ManagerDto>>>> GetAll(int hotelId)
    {
        var managers = await _managerService.GetAllAsync(hotelId);
        return Ok(ApiResponse<IReadOnlyList<ManagerDto>>.SuccessResponse(managers));
    }

    [HttpGet("{managerId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<ApiResponse<ManagerDto>>> GetById(int hotelId, Guid managerId)
    {
        var manager = await _managerService.GetByIdAsync(hotelId, managerId);
        return Ok(ApiResponse<ManagerDto>.SuccessResponse(manager));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<ManagerDto>>> Create(int hotelId, CreateManagerDto dto)
    {
        var manager = await _managerService.CreateAsync(hotelId, dto);
        return CreatedAtAction(nameof(GetById), new { hotelId, managerId = manager.Id }, ApiResponse<ManagerDto>.SuccessResponse(manager));
    }

    [HttpPut("{managerId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<ManagerDto>>> Update(int hotelId, Guid managerId, UpdateManagerDto dto)
    {
        var manager = await _managerService.UpdateAsync(hotelId, managerId, dto);
        return Ok(ApiResponse<ManagerDto>.SuccessResponse(manager));
    }

    [HttpDelete("{managerId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int hotelId, Guid managerId)
    {
        await _managerService.DeleteAsync(hotelId, managerId);
        return NoContent();
    }
}
