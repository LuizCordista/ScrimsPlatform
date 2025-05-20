using UserService.CustomException;
using UserService.Model;
using UserService.Repository;

namespace UserService.Service;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<User> CreateUserAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Username and email are required.");

        var existingUser = await userRepository.GetUserByUsernameAsync(user.Username);
        if (existingUser != null) throw new UserAlreadyExistsException("User with this username already exists.");

        existingUser = await userRepository.GetUserByEmailAsync(user.Email);
        if (existingUser != null) throw new UserAlreadyExistsException("User with this email already exists.");

        return await userRepository.CreateUserAsync(user);
    }
}