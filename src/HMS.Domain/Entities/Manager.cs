namespace HMS.Domain.Entities;

public class Manager
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;
}
