using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
using Xunit;

namespace UserService.Tests.Controller;

public class UserControllerIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        ClearUsersTable();
    }

    private void ClearUsersTable()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();
    }

    private static StringContent AsJson(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task Register_Should_Create_User()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "123" });

        var response = await _client.PostAsync("/api/user/register", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {responseBody}");
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Username_Or_Email_Is_Missing()
    {
        var content = AsJson(new { username = "", email = "test@email.com", password = "123" });

        var response = await _client.PostAsync("/api/user/register", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Username and email are required", responseBody);
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_When_Username_Exists()
    {
        var content1 = AsJson(new { username = "testuser", email = "unique@email.com", password = "123" });
        await _client.PostAsync("/api/user/register", content1);

        var content2 = AsJson(new { username = "testuser", email = "other@email.com", password = "123" });
        var response = await _client.PostAsync("/api/user/register", content2);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("User with this username already exists", responseBody);
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_When_Email_Exists()
    {
        var content1 = AsJson(new { username = "uniqueuser", email = "test@email.com", password = "123" });
        await _client.PostAsync("/api/user/register", content1);

        var content2 = AsJson(new { username = "otheruser", email = "test@email.com", password = "123" });
        var response = await _client.PostAsync("/api/user/register", content2);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("User with this email already exists", responseBody);
    }
}