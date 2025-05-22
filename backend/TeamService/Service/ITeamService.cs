using System;
using TeamService.Model;

namespace TeamService.Service;

public interface ITeamService
{
    Task<Team> CreateTeamAsync(string name, string tag, string description, Guid ownerId);
}
