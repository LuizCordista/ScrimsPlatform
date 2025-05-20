using UserService.Model;

namespace UserService.Service;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
}