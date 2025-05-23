namespace UserService.Core.DTOs;
using System.ComponentModel.DataAnnotations;

public record LoginRequestDto(
    [Required]
    [EmailAddress]
    string Email,
    [Required]
    string Password
);