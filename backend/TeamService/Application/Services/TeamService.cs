using TeamService.Core.Domain;
using TeamService.Core.Exceptions;
using TeamService.Core.Ports;

namespace TeamService.Application.Services;

public class TeamService(ITeamRepository teamRepository, IUserServiceClient userServiceClient) : ITeamService
{
    public async Task<Team> CreateTeamAsync(string name, string tag, string description, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 6 || name.Length > 60)
            throw new ArgumentException("Name must be between 6 and 60 characters.");
        if (string.IsNullOrWhiteSpace(tag) || tag.Length != 3)
            throw new ArgumentException("Tag must be exactly 3 characters.");

        if (await userServiceClient.UserExistsAsync(ownerId) == false)
            throw new UserNotFoundException("Owner does not exist.");

        if (await teamRepository.GetTeamByNameAsync(name) != null)
            throw new TeamAlreadyExistsException("A team with this name already exists.");

        var team = new Team(name, tag, description, ownerId);

        await teamRepository.AddTeamAsync(team);

        return team;
    }

    public async Task<Team> GetTeamByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Team ID cannot be empty.");

        var team = await teamRepository.GetTeamByIdAsync(id) ?? throw new TeamNotFoundException("Team not found.");

        return team;
    }
}
