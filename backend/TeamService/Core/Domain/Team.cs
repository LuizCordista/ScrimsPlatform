using System.ComponentModel.DataAnnotations;

namespace TeamService.Core.Domain;

public class Team
{
    public Guid Id { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(60)]
    public string Name { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string Tag { get; set; }

    [Required]
    public Guid OwnerId { get; set; }

    public List<Guid> MemberIds { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Team(string name, string tag, string description, Guid ownerId)
    {
        Name = name;
        Tag = tag;
        Description = description;
        OwnerId = ownerId;
        MemberIds = [ownerId];
    }
}
