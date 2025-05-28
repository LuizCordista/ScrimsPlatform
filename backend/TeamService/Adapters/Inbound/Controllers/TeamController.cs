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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeamById(Guid id)
    {
        var team = await teamService.GetTeamByIdAsync(id);

        return Ok(new GetTeamResponseDto(
            team.Id,
            team.Name,
            team.Tag,
            team.Description,
            team.OwnerId,
            team.CreatedAt,
            team.UpdatedAt
        ));
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] string? tag = null)
    {
        var (teams, totalCount) = await teamService.GetTeamsAsync(page, pageSize, name, tag);
        var items = teams.Select(team => new GetTeamResponseDto(
            team.Id,
            team.Name,
            team.Tag,
            team.Description,
            team.OwnerId,
            team.CreatedAt,
            team.UpdatedAt
        ));
        var response = new PagedTeamsResponseDto
        {
            Items = [.. items],
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
        return Ok(response);
    }
}
