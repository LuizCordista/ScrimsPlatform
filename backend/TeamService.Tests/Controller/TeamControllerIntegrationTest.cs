using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TeamService.Dto;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using JetBrains.Annotations;
using TeamService.Controller;
using TeamService.Client;
using Microsoft.AspNetCore.Authentication;
using TeamService.Data;
using TeamService.Tests.Config;

namespace TeamService.Tests.Controller;

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