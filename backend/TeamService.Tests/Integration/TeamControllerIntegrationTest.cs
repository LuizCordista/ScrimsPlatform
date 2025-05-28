using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using TeamService.Adapters.Inbound.Controllers;
using TeamService.Adapters.Outbound.Clients;
using TeamService.Core.DTOs;
using TeamService.Core.Ports;
using TeamService.Infrastructure.Data;
using TeamService.Tests.TestsConfiguration;

namespace TeamService.Tests.Integration;

[TestSubject(typeof(TeamController))]
public class TeamControllerIntegrationTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WireMockServer _wireMockServer;
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public TeamControllerIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _wireMockServer = WireMockServer.Start();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
                {
                    client.BaseAddress = new Uri(_wireMockServer.Url!);
                });
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });
            });
        });
        _client = _factory.CreateClient();
        ClearTeamsTable();
    }

    private void ClearTeamsTable()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
        dbContext.Database.EnsureCreated();
        dbContext.Teams.RemoveRange(dbContext.Teams);
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateTeam_Should_Create_Team_When_User_Exists()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "INT",
            Description: "Team for integration test"
        );

        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Integration Team", responseBody);
        Assert.Contains("INT", responseBody);
        Assert.Contains("Team for integration test", responseBody);
        Assert.Contains("ownerId", responseBody);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_User_Does_Not_Exist()
    {
        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "INT",
            Description: "Team for integration test"
        );

        var token = GenerateFakeJwt(Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Already_Exists()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "INT",
            Description: "Team for integration test"
        );

        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        await _client.SendAsync(request);

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        secondRequest.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Name_Is_Empty()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );
        var createDto = new CreateTeamRequestDto(
            Name: "",
            Tag: "INT",
            Description: "Team for integration test"
        );
        
        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Name_Is_Bigger_Than_60_Characters()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );
        var createDto = new CreateTeamRequestDto(
            Name: "This is a very long team name that exceeds the maximum length allowed",
            Tag: "INT",
            Description: "Team for integration test"
        );
        
        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Name_Is_Smaller_Than_6_Characters()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );
        var createDto = new CreateTeamRequestDto(
            Name: "Short",
            Tag: "INT",
            Description: "Team for integration test"
        );
        
        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
        

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Tag_Is_Empty()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "",
            Description: "Team for integration test"
            );

        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Tag_Is_Bigger_Than_3_Characters()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "LONG",
            Description: "Team for integration test"
        );

        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Should_Return_BadRequest_When_Team_Tag_Is_Smaller_Than_3_Characters()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "SM",
            Description: "Team for integration test"
        );

        var token = GenerateFakeJwt(ownerId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamById_Should_Return_Team_When_Exists()
    {
        var ownerId = Guid.NewGuid();

        _wireMockServer.Given(
            Request.Create().WithPath($"/api/user/*").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}")
        );

        var createDto = new CreateTeamRequestDto(
            Name: "Integration Team",
            Tag: "INT",
            Description: "Team for integration test"
        );
        var token = GenerateFakeJwt(ownerId);

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        createRequest.Headers.Add("Authorization", $"Bearer {token}");

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTeamJson = await createResponse.Content.ReadAsStringAsync();
        var createdTeam = JsonSerializer.Deserialize<CreateTeamResponseDto>(createdTeamJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.False(createdTeam == null, $"Deserialization of CreateTeamResponseDto failed. Response: {createdTeamJson}");

        var getResponse = await _client.GetAsync($"/api/team/{createdTeam.Id}");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Contains("Integration Team", responseBody);
        Assert.Contains("INT", responseBody);
        Assert.Contains("Team for integration test", responseBody);
        Assert.Contains("ownerId", responseBody);
        Assert.Contains("id", responseBody);
    }

    [Fact]
    public async Task GetTeamById_Should_Return_NotFound_When_Team_Does_Not_Exist()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/team/{nonExistentId}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTeams_Should_Return_Paginated_List()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(Request.Create().WithPath($"/api/user/*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}"));
        var token = GenerateFakeJwt(ownerId);

        for (int i = 1; i <= 3; i++)
        {
            var createDto = new CreateTeamRequestDto($"Team {i}", $"T{i}A", $"Description {i}");
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
            {
                Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        var responsePage = await _client.GetAsync("/api/team?page=1&pageSize=10");
        var json = await responsePage.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, responsePage.StatusCode);
        Assert.Contains("items", json.ToLower());
        Assert.Contains("totalcount", json.ToLower());

        Assert.Contains("team 1", json.ToLower());
        Assert.Contains("team 2", json.ToLower());
        Assert.Contains("team 3", json.ToLower());

        Assert.Contains("t1a", json.ToLower());
        Assert.Contains("t2a", json.ToLower());
        Assert.Contains("t3a", json.ToLower());

        Assert.Contains("description 1", json.ToLower());
        Assert.Contains("description 2", json.ToLower());
        Assert.Contains("description 3", json.ToLower());
    }

    [Fact]
    public async Task GetTeams_Should_Filter_By_Name_And_Tag()
    {
        var ownerId = Guid.NewGuid();
        _wireMockServer.Given(Request.Create().WithPath($"/api/user/*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody($"{{ \"id\": \"{ownerId}\", \"username\": \"testuser\", \"email\": \"test@email.com\" }}"));
        var token = GenerateFakeJwt(ownerId);

        for (int i = 1; i <= 3; i++)
        {
            var createDto = new CreateTeamRequestDto($"{i} Team", $"{i}TA", $"Description {i}");
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/create")
            {
                Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        var responsePage = await _client.GetAsync("/api/team?page=1&pageSize=10&name=1&tag=1TA");
        var json = await responsePage.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, responsePage.StatusCode);
        Assert.Contains("items", json.ToLower());
        Assert.Contains("totalcount", json.ToLower());

        Assert.Contains("1 team", json.ToLower());
        Assert.DoesNotContain("2 team", json.ToLower());
        Assert.DoesNotContain("3 team", json.ToLower());

        Assert.Contains("1ta", json.ToLower());
        Assert.DoesNotContain("2ta", json.ToLower());
        Assert.DoesNotContain("3ta", json.ToLower());

        Assert.Contains("description 1", json.ToLower());
        Assert.DoesNotContain("description 2", json.ToLower());
        Assert.DoesNotContain("description 3", json.ToLower());
    }
    private static string GenerateFakeJwt(Guid userId)
    {
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJOYW1lSWQiOiI" + userId + "ifQ.sometestsignature";
    }

    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }
}