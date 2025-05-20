using System.ComponentModel.DataAnnotations;

namespace UserService.Model;

public class User
{
    public User(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }

    public Guid Id { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    public string Username { get; set; }

    [Required] [EmailAddress] public string Email { get; set; }

    [Required] public string Password { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}