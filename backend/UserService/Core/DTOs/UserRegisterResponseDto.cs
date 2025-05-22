namespace UserService.Core.DTOs;

public record UserRegisterResponseDto(Guid Id, string Username, string Email, DateTime CreatedAt);