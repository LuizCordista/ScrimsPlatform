using System;
using Microsoft.EntityFrameworkCore;
using TeamService.Core.Domain;

namespace TeamService.Infrastructure.Data;

public class TeamDbContext(DbContextOptions<TeamDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams { get; set; }
}
