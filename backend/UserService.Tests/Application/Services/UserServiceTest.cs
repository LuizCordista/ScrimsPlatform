using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Core.Domain;
using UserService.Core.Exceptions;
using UserService.Core.Ports;
using UserService.Infrastructure.Security;
using Xunit;

namespace UserService.Tests.Application.Services;

[TestSubject(typeof(UserService.Application.Services.UserService))]
public class UserServiceTest
{
    [Fact]
    public async Task CreateUserAsync_Should_Create_User_When_Valid_And_Not_Exists()
    {
        var user = new User("testuser", "test@email.com", "123");
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync((User)null);
        repoMock.Setup(r => r.GetUserByEmailAsync(user.Email)).ReturnsAsync((User)null);
        repoMock.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(user);

        var configMock = new Mock<IConfiguration>();
        var passwordHasher = new PasswordHasher();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasher, configMock.Object);

        var result = await service.CreateUserAsync(user);

        Assert.Equal(user, result);
        repoMock.Verify(r => r.CreateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_Should_Throw_When_Username_Or_Email_Is_Empty()
    {
        var user = new User("", "", "123");
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateUserAsync(user));
    }

    [Fact]
    public async Task CreateUserAsync_Should_Throw_When_Username_Exists()
    {
        var user = new User("testuser", "test@email.com", "123");
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync(user.Username))
            .ReturnsAsync(new User("existinguser", "existing@email.com", "pass"));
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.CreateUserAsync(user));
    }

    [Fact]
    public async Task CreateUserAsync_Should_Throw_When_Email_Exists()
    {
        var user = new User("testuser", "test@email.com", "123");
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync((User)null);
        repoMock.Setup(r => r.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(new User("existinguser", "existing@email.com", "pass"));
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.CreateUserAsync(user));
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Token_When_Valid_Credentials()
    {
        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword("password");

        var user = new User("testuser", "test@email.com", hashedPassword);

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("testkeylongenoughverybigkeyforjwt");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("testissuer");
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasher, configMock.Object);

        var result = await service.LoginAsync(user.Email, "password");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
        Assert.NotNull(result.Token);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_User_Not_Found()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByEmailAsync("nonexistentemail@.com")).ReturnsAsync((User)null);
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.LoginAsync("nonexistentemail@.com", "password"));
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Invalid_Password()
    {
        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword("password");

        var user = new User("testuser", "test@email.com", hashedPassword);

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("testkeylongenoughverybigkeyforjwt");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("testissuer");
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasher, configMock.Object);

        await Assert.ThrowsAsync<InvalidPasswordException>(() => service.LoginAsync(user.Email, "wrongpassword"));
    }

    [Fact]
    public async Task GetAllUsersAsync_Should_Return_All_Users()
    {
        var users = new List<User>
        {
            new User("user1", "user1@email.com", "password"),
            new User("user2", "user2@email.com", "password")
        };
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        var result = await service.GetAllUsersAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Username == "user1");
        Assert.Contains(result, u => u.Username == "user2");
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Return_User_When_Exists()
    {
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@email.com", "password");

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        var result = await service.GetUserByIdAsync(userId);

        Assert.Equal(user, result);
        repoMock.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Throw_When_Id_Is_Empty()
    {
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Throw_When_User_Not_Found()
    {
        var userId = Guid.NewGuid();

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User)null);

        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetUserByIdAsync(userId));
    }

    [Fact]
    public async Task SearchUserByUsernameAsync_Should_Return_Users_When_Exists()
    {
        var username = "testuser";
        var user = new User(username, "test@email.com", "password");

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.SearchUsersByUsernameAsync(username)).ReturnsAsync(new List<User> { user });
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        var result = await service.SearchUserByUsernameAsync(username);

        Assert.Single(result);
        Assert.Equal(user.Username, result[0].Username);
        Assert.Equal(user.Email, result[0].Email);
    }

    [Fact]
    public async Task SearchUserByUsernameAsync_Should_Return_Empty_When_Not_Exists()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.SearchUsersByUsernameAsync("username")).ReturnsAsync(new List<User>());
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        var result = await service.SearchUserByUsernameAsync("username");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchUserByUsernameAsync_Should_Return_Multiple_When_Exists()
    {
        var user1 = new User("username1", "test1@email.com", "password");
        var user2 = new User("username2", "test2@email.com", "password");

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.SearchUsersByUsernameAsync("user")).ReturnsAsync(new List<User> { user1, user2 });
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        var result = await service.SearchUserByUsernameAsync("user");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchUserByUsernameAsync_Should_Throw_When_Username_Is_Empty()
    {
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchUserByUsernameAsync(""));
    }

    [Fact]
    public async Task UpdateuserPasswordAsync_Should_Return_True_When_Valid()
    {
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@email.com", "password");
        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword("password");
        user.Password = hashedPassword;
        var newPassword = "newpassword";

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
        repoMock.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);

        var configMock = new Mock<IConfiguration>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasher, configMock.Object);

        var result = await service.UpdateUserPasswordAsync(userId, "password", newPassword);

        Assert.True(result);
        repoMock.Verify(r => r.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateuserPasswordAsync_Should_Throw_When_Id_Is_Empty()
    {
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateUserPasswordAsync(Guid.Empty, "password", "newpassword"));
    }

    [Fact]
    public async Task UpdateuserPasswordAsync_Should_Throw_When_Password_Is_Empty()
    {
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateUserPasswordAsync(userId, "", "newpassword"));
    }

    [Fact]
    public async Task UpdateuserPasswordAsync_Should_Throw_When_User_Not_Found()
    {
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User)null);

        var configMock = new Mock<IConfiguration>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasherMock.Object, configMock.Object);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.UpdateUserPasswordAsync(userId, "password", "newpassword"));
    }

    [Fact]
    public async Task UpdateuserPasswordAsync_Should_Throw_When_Invalid_Current_Password()
    {
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@email.com", "password");
        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword("password");
        user.Password = hashedPassword;

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

        var configMock = new Mock<IConfiguration>();
        var service = new UserService.Application.Services.UserService(repoMock.Object, passwordHasher, configMock.Object);

        await Assert.ThrowsAsync<InvalidPasswordException>(() => service.UpdateUserPasswordAsync(userId, "wrongpassword", "newpassword"));
    }
}