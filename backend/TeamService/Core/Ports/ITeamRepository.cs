using System.Collections.Generic;
using System.Threading.Tasks;
using TeamService.Core.Domain;

namespace TeamService.Core.Ports;

public interface ITeamRepository
{
    Task<Team> AddTeamAsync(Team team);
    Task<Team?> GetTeamByIdAsync(Guid id);
    Task<Team?> GetTeamByNameAsync(string name);
    Task UpdateTeamAsync(Team team);
    Task DeleteTeamAsync(Guid id);
    Task<(IEnumerable<Team> Teams, int TotalCount)> GetTeamsAsync(int page, int pageSize, string? name, string? tag);
}
