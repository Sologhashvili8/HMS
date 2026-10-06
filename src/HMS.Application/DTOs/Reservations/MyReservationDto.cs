namespace HMS.Application.DTOs.Reservations;

public class MyReservationDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCity { get; set; } = string.Empty;
    public string HotelImageUrl { get; set; } = string.Empty;
    public List<string> RoomNames { get; set; } = new();
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
}
