using TeamService.Core.Domain;

namespace TeamService.Core.Ports;

public interface ITeamRepository
{
    Task<Team> AddTeamAsync(Team team);
    Task<Team?> GetTeamByIdAsync(Guid id);
    Task<Team?> GetTeamByNameAsync(string name);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task UpdateTeamAsync(Team team);
    Task DeleteTeamAsync(Guid id);
}
