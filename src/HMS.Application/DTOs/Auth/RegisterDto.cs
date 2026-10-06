using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Auth;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{11}$", ErrorMessage = "Personal number must be exactly 11 digits.")]
    public string PersonalNumber { get; set; } = string.Empty;

    [Required, RegularExpression(@"^5\d{8}$", ErrorMessage = "Phone number must start with 5 and be 9 digits long.")]
    public string PhoneNumber { get; set; } = string.Empty;
}
