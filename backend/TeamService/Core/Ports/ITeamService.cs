using TeamService.Core.Domain;

namespace TeamService.Core.Ports;

public interface ITeamService
{
    Task<Team> CreateTeamAsync(string name, string tag, string description, Guid ownerId);
}
