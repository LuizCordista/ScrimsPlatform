namespace TeamService.Core.DTOs;

public record CreateTeamResponseDto(
    Guid Id,
    string Name,
    string Tag,
    string Description,
    Guid OwnerId,
    DateTime CreatedAt
);
