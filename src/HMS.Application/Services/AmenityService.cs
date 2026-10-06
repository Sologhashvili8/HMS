using HMS.Application.DTOs.Amenities;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Entities;
using Mapster;

namespace HMS.Application.Services;

public class AmenityService : IAmenityService
{
    private readonly IUnitOfWork _unitOfWork;

    public AmenityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AmenityDto>> GetAllAsync()
    {
        var amenities = await _unitOfWork.Repository<Amenity>().GetAllAsync();
        return amenities.Adapt<List<AmenityDto>>();
    }
}
