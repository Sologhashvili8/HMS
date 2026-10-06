namespace HMS.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public string EncryptedCardNumber { get; set; } = string.Empty;
    public string EncryptedPhoneNumber { get; set; } = string.Empty;
    public string EncryptedPersonalNumber { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
