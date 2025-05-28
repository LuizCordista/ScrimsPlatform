using JetBrains.Annotations;
using Moq;
using TeamService.Core.Domain;
using TeamService.Core.Exceptions;
using TeamService.Core.Ports;
using Xunit;

namespace TeamService.Tests.Application.Services;

[TestSubject(typeof(TeamService.Application.Services.TeamService))]
public class TeamServiceTest
{
    [Fact]
    public async Task CreateTeamAsync_Should_Create_Team_When_Valid_And_Not_Exists()
    {
        var team = new Team("Test Team", "TTT", "This is a test team.", Guid.NewGuid());


        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByNameAsync(team.Name)).ReturnsAsync((Team?)null);
        repoMock.Setup(r => r.AddTeamAsync(team)).ReturnsAsync(team);

        var userServiceMock = new Mock<IUserServiceClient>();
        userServiceMock.Setup(u => u.UserExistsAsync(team.OwnerId)).ReturnsAsync(true);

        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        var result = await service.CreateTeamAsync(team.Name, team.Tag, team.Description, team.OwnerId);

        Assert.Equal(team.Name, result.Name);
        Assert.Equal(team.Tag, result.Tag);
        Assert.Equal(team.Description, result.Description);
        Assert.Equal(team.OwnerId, result.OwnerId);
        repoMock.Verify(r => r.AddTeamAsync(It.Is<Team>(t =>
            t.Name == team.Name &&
            t.Tag == team.Tag &&
            t.Description == team.Description &&
            t.OwnerId == team.OwnerId
        )), Times.Once);
    }

    [Fact]
    public async Task CreateTeamAsync_Should_Throw_Exception_When_Team_Already_Exists()
    {
        var team = new Team("Test Team", "TTT", "This is a test team.", Guid.NewGuid());

        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByNameAsync(team.Name)).ReturnsAsync(team);

        var userServiceMock = new Mock<IUserServiceClient>();
        userServiceMock.Setup(u => u.UserExistsAsync(team.OwnerId)).ReturnsAsync(true);

        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<TeamAlreadyExistsException>(() =>
            service.CreateTeamAsync(team.Name, team.Tag, team.Description, team.OwnerId));
    }

    [Fact]
    public async Task CreateTeamAsync_Should_Throw_Exception_When_Owner_Does_Not_Exist()
    {
        var team = new Team("Test Team", "TTT", "This is a test team.", Guid.NewGuid());

        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByNameAsync(team.Name)).ReturnsAsync((Team?)null);

        var userServiceMock = new Mock<IUserServiceClient>();
        userServiceMock.Setup(u => u.UserExistsAsync(team.OwnerId)).ReturnsAsync(false);

        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.CreateTeamAsync(team.Name, team.Tag, team.Description, team.OwnerId));
    }

    [Fact]
    public async Task CreateTeamAsync_Should_Throw_Exception_When_Name_Is_Invalid()
    {
        var team = new Team("Test", "TTT", "This is a test team.", Guid.NewGuid());

        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByNameAsync(team.Name)).ReturnsAsync((Team?)null);

        var userServiceMock = new Mock<IUserServiceClient>();
        userServiceMock.Setup(u => u.UserExistsAsync(team.OwnerId)).ReturnsAsync(true);

        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(team.Name, team.Tag, team.Description, team.OwnerId));
    }

    [Fact]
    public async Task CreateTeamAsync_Should_Throw_Exception_When_Tag_Is_Invalid()
    {
        var team = new Team("Test Team", "T", "This is a test team.", Guid.NewGuid());

        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByNameAsync(team.Name)).ReturnsAsync((Team?)null);

        var userServiceMock = new Mock<IUserServiceClient>();
        userServiceMock.Setup(u => u.UserExistsAsync(team.OwnerId)).ReturnsAsync(true);

        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(team.Name, team.Tag, team.Description, team.OwnerId));
    }

    [Fact]
    public async Task GetTeamByIdAsync_Should_Return_Team_When_Exists()
    {
        var teamId = Guid.NewGuid();
        var team = new Team("Test Team", "TTT", "This is a test team.", Guid.NewGuid());

        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByIdAsync(teamId)).ReturnsAsync(team);
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        var result = await service.GetTeamByIdAsync(teamId);

        Assert.Equal(team, result);
        repoMock.Verify(r => r.GetTeamByIdAsync(teamId), Times.Once);
    }

    [Fact]
    public async Task GetTeamByIdAsync_Should_Throw_ArgumentException_When_Id_Is_Empty()
    {
        var repoMock = new Mock<ITeamRepository>();
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTeamByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetTeamByIdAsync_Should_Throw_TeamNotFoundException_When_Team_Does_Not_Exist()
    {
        var teamId = Guid.NewGuid();
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamByIdAsync(teamId)).ReturnsAsync((Team?)null);
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        await Assert.ThrowsAsync<TeamNotFoundException>(() => service.GetTeamByIdAsync(teamId));
    }

    [Fact]
    public async Task GetTeamsAsync_Should_Return_Paginated_And_Filtered_Teams()
    {
        var teams = new List<Team>
        {
            new Team("Alpha Team", "ALP", "First team", Guid.NewGuid()),
            new Team("Beta Team", "BET", "Second team", Guid.NewGuid()),
            new Team("Gamma Team", "GAM", "Third team", Guid.NewGuid())
        };
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamsAsync(1, 2, null, null))
            .ReturnsAsync((teams.Take(2), teams.Count));
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        var (resultTeams, totalCount) = await service.GetTeamsAsync(1, 2, null, null);

        Assert.Equal(2, resultTeams.Count());
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetTeamsAsync_Should_Filter_By_Name_And_Tag()
    {
        var teams = new List<Team>
        {
            new Team("Alpha Team", "ALP", "First team", Guid.NewGuid()),
            new Team("Beta Team", "BET", "Second team", Guid.NewGuid())
        };
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamsAsync(1, 10, "Alpha", "ALP"))
            .ReturnsAsync((teams.Where(t => t.Name.Contains("Alpha") && t.Tag.Contains("ALP")), 1));
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        var (resultTeams, totalCount) = await service.GetTeamsAsync(1, 10, "Alpha", "ALP");

        Assert.Single(resultTeams);
        Assert.Equal("Alpha Team", resultTeams.First().Name);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetTeamsAsync_Should_Return_Empty_When_No_Teams_Match_Filter()
    {
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetTeamsAsync(1, 10, "NonExistent", null))
            .ReturnsAsync((Enumerable.Empty<Team>(), 0));
        var userServiceMock = new Mock<IUserServiceClient>();
        var service = new TeamService.Application.Services.TeamService(repoMock.Object, userServiceMock.Object);

        var (resultTeams, totalCount) = await service.GetTeamsAsync(1, 10, "NonExistent", null);

        Assert.Empty(resultTeams);
        Assert.Equal(0, totalCount);
    }
}
