using Microsoft.EntityFrameworkCore;
using System.Linq;
using TeamService.Core.Domain;
using TeamService.Core.Ports;
using TeamService.Infrastructure.Data;

namespace TeamService.Adapters.Outbound.Repositories;

public class TeamRepository(TeamDbContext teamDbContext) : ITeamRepository
{
    public async Task<Team> AddTeamAsync(Team team)
    {
        await teamDbContext.Teams.AddAsync(team);
        await teamDbContext.SaveChangesAsync();
        return team;
    }

    public async Task<Team?> GetTeamByIdAsync(Guid id)
    {
        return await teamDbContext.Teams.FindAsync(id);
    }

    public async Task<Team?> GetTeamByNameAsync(string name)
    {
        return await teamDbContext.Teams.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task UpdateTeamAsync(Team team)
    {
        teamDbContext.Teams.Update(team);
        await teamDbContext.SaveChangesAsync();
    }

    public async Task DeleteTeamAsync(Guid id)
    {
        var team = await teamDbContext.Teams.FindAsync(id);
        if (team != null)
        {
            teamDbContext.Teams.Remove(team);
            await teamDbContext.SaveChangesAsync();
        }
    }

    public async Task<(IEnumerable<Team> Teams, int TotalCount)> GetTeamsAsync(int page, int pageSize, string? name, string? tag)
    {
        var query = teamDbContext.Teams.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(t => t.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(t => t.Tag.Contains(tag));
        var totalCount = await query.CountAsync();
        var teams = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (teams, totalCount);
    }
}
