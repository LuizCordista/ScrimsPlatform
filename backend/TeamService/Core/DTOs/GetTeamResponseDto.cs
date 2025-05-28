namespace TeamService.Core.DTOs;

public record GetTeamResponseDto(
    Guid Id,
    string Name,
    string Tag,
    string Description,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

