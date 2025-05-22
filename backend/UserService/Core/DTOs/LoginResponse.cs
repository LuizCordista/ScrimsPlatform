namespace UserService.Core.DTOs;

public record LoginResponse(string Token, DateTime ExpiresAt, Guid Id, string Username, string Email);