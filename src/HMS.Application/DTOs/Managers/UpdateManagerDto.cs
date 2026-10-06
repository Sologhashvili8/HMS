using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Managers;

public class UpdateManagerDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}
