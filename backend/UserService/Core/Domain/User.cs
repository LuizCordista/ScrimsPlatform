using System.ComponentModel.DataAnnotations;

namespace UserService.Core.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public User(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }
}