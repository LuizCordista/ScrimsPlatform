namespace UserService.Core.Ports;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool verifyPassword(string password, string hashedPassword);
}