namespace HMS.Domain.Entities;

public class RoomPhoto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
}
