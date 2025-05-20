using UserService.Dto;
using UserService.Model;

namespace UserService.Service;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<User> GetUserByIdAsync(Guid id);
}