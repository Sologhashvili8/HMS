using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Reservations;

public class CreatePaymentDto
{
    [Required, RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be exactly 16 digits.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required, RegularExpression(@"^5\d{8}$", ErrorMessage = "Phone number must start with 5 and be 9 digits long.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{11}$", ErrorMessage = "Personal number must be exactly 11 digits.")]
    public string PersonalNumber { get; set; } = string.Empty;
}
