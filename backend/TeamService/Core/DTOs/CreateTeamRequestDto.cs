namespace TeamService.Core.DTOs;
using System.ComponentModel.DataAnnotations;

public record CreateTeamRequestDto(
    [Required]
    [MinLength(6)]
    [MaxLength(60)]
    string Name,
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    string Tag,
    string Description
);
