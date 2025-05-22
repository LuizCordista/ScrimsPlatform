namespace TeamService.Dto;

public record CreateTeamRequestDto(
    string Name,
    string Tag,
    string Description
);
