namespace UserService.Core.DTOs;
using System.ComponentModel.DataAnnotations;

public record UserRegisterRequestDto(
    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    string Username,
    [Required]
    [EmailAddress]
    string Email,
    [Required]
    string Password
);