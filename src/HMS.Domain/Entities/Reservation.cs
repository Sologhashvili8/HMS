namespace HMS.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();

    public Payment? Payment { get; set; }
}
