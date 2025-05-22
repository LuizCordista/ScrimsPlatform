using System;
using System.ComponentModel.DataAnnotations;

namespace TeamService.Model;

public class Team
{
    public Guid Id { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(60)]
    public required string Name { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public required string Tag { get; set; }

    [Required]
    public Guid OwnerId { get; set; }

    public List<Guid> MemberIds { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
