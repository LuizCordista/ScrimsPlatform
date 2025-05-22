namespace UserService.Core.DTOs;

public record UpdatePasswordRequestDto(string CurrentPassword, string NewPassword);