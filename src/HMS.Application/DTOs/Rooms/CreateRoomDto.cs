using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Rooms;

public class CreateRoomDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    public List<int> AmenityIds { get; set; } = new();
    public List<string> PhotoUrls { get; set; } = new();
}
