using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Guests;

public class UpdateGuestDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^5\d{8}$", ErrorMessage = "Phone number must start with 5 and be 9 digits long.")]
    public string PhoneNumber { get; set; } = string.Empty;
}
