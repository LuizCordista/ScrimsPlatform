using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.CustomException;
using UserService.Dto;
using UserService.Model;
using UserService.Repository;

namespace UserService.Service;

public class UserService(IUserRepository userRepository, IConfiguration configuration) : IUserService
{
    public async Task<User> CreateUserAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Username and email are required.");

        var existingUser = await userRepository.GetUserByUsernameAsync(user.Username);
        if (existingUser != null) throw new UserAlreadyExistsException("User with this username already exists.");

        existingUser = await userRepository.GetUserByEmailAsync(user.Email);
        if (existingUser != null) throw new UserAlreadyExistsException("User with this email already exists.");

        var passwordHasher = new PasswordHasher();

        user.Password = passwordHasher.HashPassword(user.Password);

        return await userRepository.CreateUserAsync(user);
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var user = await userRepository.GetUserByEmailAsync(email);
        if (user == null) throw new UserNotFoundException("User not found.");
        var passwordHasher = new PasswordHasher();
        if (!passwordHasher.verifyPassword(password, user.Password))
            throw new InvalidPasswordException("Invalid password.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            null,
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), DateTime.UtcNow.AddDays(7),
            user.Id, user.Username, user.Email);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllUsersAsync();
        return users.ToList();
    }

    public async Task<User> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Invalid user ID.");

        var user = await userRepository.GetUserByIdAsync(id);

        if (user == null) throw new UserNotFoundException("User not found.");

        return user;
    }

    public async Task<List<User>> SearchUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.");

        return await userRepository.SearchUsersByUsernameAsync(username);
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        if (id == Guid.Empty) throw new ArgumentException("Invalid user ID.");
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Current and new passwords are required.");

        var user = await userRepository.GetUserByIdAsync(id) ?? throw new UserNotFoundException("User not found.");
        
        var passwordHasher = new PasswordHasher();
        if (!passwordHasher.verifyPassword(currentPassword, user.Password))
            throw new InvalidPasswordException("Invalid current password.");

        var newPasswordHash = passwordHasher.HashPassword(newPassword);
        user.Password = newPasswordHash;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateUserAsync(user);
        return true;
    }
}