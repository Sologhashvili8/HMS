namespace HMS.Application.DTOs.Reservations;

public class ReservationDto
{
    public int Id { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public Guid GuestId { get; set; }
    public List<int> RoomIds { get; set; } = new();
}
