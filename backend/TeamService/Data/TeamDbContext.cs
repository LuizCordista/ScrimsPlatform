using System;
using Microsoft.EntityFrameworkCore;
using TeamService.Model;

namespace TeamService.Data;

public class TeamDbContext(DbContextOptions<TeamDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams { get; set; }
}
