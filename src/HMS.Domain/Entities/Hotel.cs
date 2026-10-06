namespace HMS.Domain.Entities;

public class Hotel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Manager> Managers { get; set; } = new List<Manager>();
}
