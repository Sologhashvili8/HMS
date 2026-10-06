using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Reservations;

public class CreateReservationDto
{
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one room must be selected.")]
    public List<int> RoomIds { get; set; } = new();

    [Required]
    public CreatePaymentDto Payment { get; set; } = new();
}
