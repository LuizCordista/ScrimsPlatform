using System;
using TeamService.Model;

namespace TeamService.Repository;

public interface ITeamRepository
{
    Task<Team> AddTeamAsync(Team team);
    Task<Team?> GetTeamByIdAsync(int id);
    Task<Team?> GetTeamByNameAsync(string name);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task UpdateTeamAsync(Team team);
    Task DeleteTeamAsync(int id);
}
