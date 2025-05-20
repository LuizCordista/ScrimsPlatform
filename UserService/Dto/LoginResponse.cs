namespace UserService.Dto;

public record LoginResponse(string Token, DateTime ExpiresAt, string Username, string Email);