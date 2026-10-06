using HMS.Application.DTOs.Hotels;

namespace HMS.Application.DTOs.Admin;

public class AdminHotelDto : HotelDto
{
    public int RoomsCount { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
}
