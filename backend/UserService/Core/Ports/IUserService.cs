using UserService.Core.Domain;
using UserService.Core.DTOs;

namespace UserService.Core.Ports;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<List<User>> GetAllUsersAsync();
    Task<User> GetUserByIdAsync(Guid id);
    Task<List<User>> SearchUserByUsernameAsync(string username);
    Task<bool> UpdateUserPasswordAsync(Guid id, string currentPassword,string newPassword);
}