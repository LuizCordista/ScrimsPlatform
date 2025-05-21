using UserService.Dto;
using UserService.Model;

namespace UserService.Service;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<List<User>> GetAllUsersAsync();
    Task<User> GetUserByIdAsync(Guid id);
    Task<List<User>> SearchUserByUsernameAsync(string username);
    Task<bool> UpdateUserPasswordAsync(Guid id, string currentPassword,string newPassword);
}