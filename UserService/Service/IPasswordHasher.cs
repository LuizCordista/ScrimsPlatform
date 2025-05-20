namespace UserService.Service;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool verifyPassword(string password, string hashedPassword);
}