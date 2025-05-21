using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
using Xunit;
using System.Linq;
using System.Collections.Generic;

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

    [Fact]
    public async Task Login_Should_Return_Token_When_Valid_Credentials()
    {
        var content1 = AsJson(new { username = "testuser", email = "test@email.com", password = "123" });
        await _client.PostAsync("/api/user/register", content1);

        var content2 = AsJson(new { email = "test@email.com", password = "123" });
        var response = await _client.PostAsync("/api/user/login", content2);

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {responseBody}");
        Assert.Contains("token", responseBody);
        Assert.Contains("expiresAt", responseBody);
        Assert.Contains("id", responseBody);
        Assert.Equal("testuser", root.GetProperty("username").GetString());
        Assert.Equal("test@email.com", root.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_Should_Return_NotFound_When_User_Dont_Exists()
    {
        var content = AsJson(new { email = "nonexistent@email.com", password = "password" });
        var response = await _client.PostAsync("/api/user/login", content);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Return_BadRequest_When_Invalid_Credentials()
    {
        var content1 = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        await _client.PostAsync("/api/user/register", content1);

        var content2 = AsJson(new { email = "test@email.com", password = "wrongpassword" });
        var response = await _client.PostAsync("/api/user/login", content2);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_Should_Return_User_When_Exists()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        var response = await _client.PostAsync("/api/user/register", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseBody);
        var userId = doc.RootElement.GetProperty("id").GetGuid();

        var getUserResponse = await _client.GetAsync($"/api/user/{userId}");
        var getUserResponseBody = await getUserResponse.Content.ReadAsStringAsync();
        var getUserDoc = JsonDocument.Parse(getUserResponseBody);
        var getUserRoot = getUserDoc.RootElement;

        Assert.True(getUserResponse.IsSuccessStatusCode,
            $"Status: {getUserResponse.StatusCode}, Body: {getUserResponseBody}");
        Assert.Equal("testuser", getUserRoot.GetProperty("username").GetString());
        Assert.Equal("test@email.com", getUserRoot.GetProperty("email").GetString());
        Assert.Equal(userId, getUserRoot.GetProperty("id").GetGuid());
        Assert.Contains("createdAt", getUserResponseBody);
        Assert.Contains("updatedAt", getUserResponseBody);
    }

    [Fact]
    public async Task GetUserById_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var nonExistentUserId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/user/{nonExistentUserId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("User not found", responseBody);
    }

    [Fact]
    public async Task GetAuthenticatedUser_Should_Return_User_When_Authenticated()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        var response = await _client.PostAsync("/api/user/register", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseBody);
        var userId = doc.RootElement.GetProperty("id").GetGuid();

        var loginContent = AsJson(new { email = "test@email.com", password = "password" });
        var loginResponse = await _client.PostAsync("/api/user/login", loginContent);
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginResponseBody);
        var token = loginDoc.RootElement.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var getUserResponse = await _client.GetAsync("/api/user/me");
        var getUserResponseBody = await getUserResponse.Content.ReadAsStringAsync();
        var getUserDoc = JsonDocument.Parse(getUserResponseBody);
        var getUserRoot = getUserDoc.RootElement;

        Assert.True(getUserResponse.IsSuccessStatusCode,
            $"Status: {getUserResponse.StatusCode}, Body: {getUserResponseBody}");
        Assert.Equal("testuser", getUserRoot.GetProperty("username").GetString());
        Assert.Equal("test@email.com", getUserRoot.GetProperty("email").GetString());
        Assert.Equal(userId, getUserRoot.GetProperty("id").GetGuid());
        Assert.Contains("createdAt", getUserResponseBody);
        Assert.Contains("updatedAt", getUserResponseBody);
    }

    [Fact]
    public async Task GetAuthenticatedUser_Should_Return_Unauthorized_When_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/user/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePassword_Should_Return_Success_When_Authenticated()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        await _client.PostAsync("/api/user/register", content);

        var loginContent = AsJson(new { email = "test@email.com", password = "password" });
        var loginResponse = await _client.PostAsync("/api/user/login", loginContent);
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginResponseBody);
        var token = loginDoc.RootElement.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updatePasswordContent = AsJson(new { currentPassword = "password", newPassword = "newpassword" });
        var updatePasswordResponse = await _client.PutAsync("/api/user/me/password", updatePasswordContent);
        var updatePasswordResponseBody = await updatePasswordResponse.Content.ReadAsStringAsync();

        Assert.True(updatePasswordResponse.IsSuccessStatusCode,
            $"Status: {updatePasswordResponse.StatusCode}, Body: {updatePasswordResponseBody}");
    }

    [Fact]
    public async Task UpdatePassword_Should_Return_Unauthorized_When_Not_Authenticated()
    {
        var updatePasswordContent = AsJson(new { currentPassword = "password", newPassword = "newpassword" });
        var response = await _client.PutAsync("/api/user/me/password", updatePasswordContent);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePassword_Should_Return_BadRequest_When_Empty_New_Password()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        await _client.PostAsync("/api/user/register", content);

        var loginContent = AsJson(new { email = "test@email.com", password = "password" });
        var loginResponse = await _client.PostAsync("/api/user/login", loginContent);
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginResponseBody);
        var token = loginDoc.RootElement.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updatePasswordContent = AsJson(new { currentPassword = "wrongpassword", newPassword = "" });
        var updatePasswordResponse = await _client.PutAsync("/api/user/me/password", updatePasswordContent);

        Assert.Equal(HttpStatusCode.BadRequest, updatePasswordResponse.StatusCode);
    }

    [Fact]
    public async Task UpdatePassword_Should_Return_Unauthorized_When_Invalid_Credentials()
    {
        var content = AsJson(new { username = "testuser", email = "test@email.com", password = "password" });
        await _client.PostAsync("/api/user/register", content);

        var loginContent = AsJson(new { email = "test@email.com", password = "password" });
        var loginResponse = await _client.PostAsync("/api/user/login", loginContent);
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginResponseBody);
        var token = loginDoc.RootElement.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updatePasswordContent = AsJson(new { currentPassword = "wrongpassword", newPassword = "newpassword" });
        var updatePasswordResponse = await _client.PutAsync("/api/user/me/password", updatePasswordContent);

        Assert.Equal(HttpStatusCode.Unauthorized, updatePasswordResponse.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByUsername_Should_Return_Users_That_Start_With_Prefix()
    {
        var user1 = new { username = "alice", email = "alice@email.com", password = "123" };
        var user2 = new { username = "alicia", email = "alicia@email.com", password = "123" };
        var user3 = new { username = "bob", email = "bob@email.com", password = "123" };
        await _client.PostAsync("/api/user/register", AsJson(user1));
        await _client.PostAsync("/api/user/register", AsJson(user2));
        await _client.PostAsync("/api/user/register", AsJson(user3));

        var response = await _client.GetAsync("/api/user/search?username=ali");
        var responseBody = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {responseBody}");
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        var usernames = users.Select(u => u.GetProperty("username").GetString()).ToList();
        Assert.Contains("alice", usernames);
        Assert.Contains("alicia", usernames);
        Assert.DoesNotContain("bob", usernames);
    }

    [Fact]
    public async Task SearchUsersByUsername_Should_Return_Empty_When_No_Match()
    {
        var user = new { username = "charlie", email = "charlie@email.com", password = "123" };
        await _client.PostAsync("/api/user/register", AsJson(user));

        var response = await _client.GetAsync("/api/user/search?username=zzz");
        var responseBody = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<JsonElement>>(responseBody);

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {responseBody}");
        Assert.NotNull(users);
        Assert.Empty(users);
    }
}