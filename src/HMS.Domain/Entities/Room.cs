namespace HMS.Domain.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }

    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();
    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    public ICollection<RoomPhoto> RoomPhotos { get; set; } = new List<RoomPhoto>();
}
