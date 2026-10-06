namespace HMS.Application.DTOs.Rooms;

public class RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int HotelId { get; set; }
    public List<string> Amenities { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
}
