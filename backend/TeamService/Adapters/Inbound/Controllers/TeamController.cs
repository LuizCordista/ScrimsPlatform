using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamService.Core.DTOs;
using TeamService.Core.Ports;

namespace TeamService.Adapters.Inbound.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TeamController(ITeamService teamService) : ControllerBase
{

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequestDto createTeamRequestDto)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null)
        {
            return Unauthorized();
        }
        Guid userId = Guid.Parse(userIdValue);

        var team = await teamService.CreateTeamAsync(createTeamRequestDto.Name, createTeamRequestDto.Tag,
            createTeamRequestDto.Description, userId);

        return CreatedAtAction(nameof(CreateTeam), new { id = team.Id }, new CreateTeamResponseDto(
                team.Id,
                team.Name,
                team.Tag,
                team.Description,
                team.OwnerId,
                team.CreatedAt
            )
        );
    }
}
