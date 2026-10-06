namespace HMS.Application.DTOs.Reservations;

public class StaffReservationDto
{
    public int Id { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public Guid GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string GuestPersonalNumber { get; set; } = string.Empty;
    public List<string> RoomNames { get; set; } = new();
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
}
