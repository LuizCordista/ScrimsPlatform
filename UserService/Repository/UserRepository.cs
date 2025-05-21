using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Model;

namespace UserService.Repository;

public class UserRepository(UserDbContext userDbContext) : IUserRepository
{
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await userDbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await userDbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await userDbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userDbContext.Users
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        userDbContext.Users.Add(user);
        await userDbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        userDbContext.Users.Update(user);
        await userDbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await userDbContext.Users.FindAsync(id);
        if (user == null) return false;
        userDbContext.Users.Remove(user);
        await userDbContext.SaveChangesAsync();
        return true;
    }

    public Task<List<User>> SearchUsersByUsernameAsync(string username)
    {
        return userDbContext.Users
            .Where(u => u.Username.Contains(username))
            .ToListAsync();
    }
}