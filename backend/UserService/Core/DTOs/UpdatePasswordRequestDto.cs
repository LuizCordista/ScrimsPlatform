namespace UserService.Dto;

public record UpdatePasswordRequestDto(string CurrentPassword, string NewPassword);