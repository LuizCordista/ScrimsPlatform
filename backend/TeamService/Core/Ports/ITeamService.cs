using System.Collections.Generic;
using TeamService.Core.Domain;

namespace TeamService.Core.Ports;

public interface ITeamService
{
    Task<Team> CreateTeamAsync(string name, string tag, string description, Guid ownerId);
    Task<Team> GetTeamByIdAsync(Guid id);
    Task<(IEnumerable<Team> Teams, int TotalCount)> GetTeamsAsync(int page, int pageSize, string? name, string? tag);
}
