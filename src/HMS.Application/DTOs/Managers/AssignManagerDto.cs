using System.ComponentModel.DataAnnotations;

namespace HMS.Application.DTOs.Managers;

public class AssignManagerDto
{
    [Required]
    public Guid UserId { get; set; }
}
