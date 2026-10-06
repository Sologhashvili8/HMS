using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Hotels;

public class UpdateHotelDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Range(1, 5)]
    public byte Rating { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
}
