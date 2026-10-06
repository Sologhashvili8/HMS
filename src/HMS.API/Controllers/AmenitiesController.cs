using HMS.Application.Common;
using HMS.Application.DTOs.Amenities;
using HMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/amenities")]
public class AmenitiesController : ControllerBase
{
    private readonly IAmenityService _amenityService;

    public AmenitiesController(IAmenityService amenityService)
    {
        _amenityService = amenityService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AmenityDto>>>> GetAll()
    {
        var amenities = await _amenityService.GetAllAsync();
        return Ok(ApiResponse<IReadOnlyList<AmenityDto>>.SuccessResponse(amenities));
    }
}
