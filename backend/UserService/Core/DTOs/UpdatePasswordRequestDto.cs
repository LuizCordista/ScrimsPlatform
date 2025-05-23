namespace UserService.Core.DTOs;
using System.ComponentModel.DataAnnotations;

public record UpdatePasswordRequestDto(
    [Required]
    string CurrentPassword,
    [Required]
    string NewPassword
);