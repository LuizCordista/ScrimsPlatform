namespace UserService.Core.DTOs;

public record UserResponseDto(Guid Id, string Username, string Email, DateTime CreatedAt, DateTime UpdatedAt);