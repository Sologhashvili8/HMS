using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Reservations;

public class CreateStaffReservationDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{11}$", ErrorMessage = "Personal number must be exactly 11 digits.")]
    public string PersonalNumber { get; set; } = string.Empty;

    [Required, RegularExpression(@"^5\d{8}$", ErrorMessage = "Phone number must start with 5 and be 9 digits long.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one room must be selected.")]
    public List<int> RoomIds { get; set; } = new();
}
