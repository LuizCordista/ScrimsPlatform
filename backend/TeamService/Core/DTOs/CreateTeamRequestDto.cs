namespace TeamService.Core.DTOs;

public record CreateTeamRequestDto(
    string Name,
    string Tag,
    string Description
);
