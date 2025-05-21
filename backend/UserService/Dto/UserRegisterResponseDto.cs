namespace UserService.Dto;

public record UserRegisterResponseDto(Guid Id, string Username, string Email, DateTime CreatedAt);