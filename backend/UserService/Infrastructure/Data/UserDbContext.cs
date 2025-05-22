using Microsoft.EntityFrameworkCore;
using UserService.Core.Domain;

namespace UserService.Infrastructure.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}