namespace TeamService.Dto;

public record CreateTeamResponseDto(
    Guid Id,
    string Name,
    string Tag,
    string Description,
    Guid ownerId,
    DateTime CreatedAt
);
