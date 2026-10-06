namespace HMS.Application.DTOs.Reservations;

public class UpdateReservationDto
{
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
}
