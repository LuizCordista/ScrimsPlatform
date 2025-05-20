using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.CustomException;
using UserService.Model;
using UserService.Repository;
using UserService.Service;
using Xunit;

namespace UserService.Tests.Service;

[TestSubject(typeof(UserService.Service.UserService))]
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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

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
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);

        await Assert.ThrowsAsync<InvalidPasswordException>(() => service.LoginAsync(user.Email, "wrongpassword"));
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_Return_User_When_Exists()
    {
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@email.com", "password");
        
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
        
        var configMock = new Mock<IConfiguration>();
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);
        
        var result = await service.GetUserByIdAsync(userId);
        
        Assert.Equal(user, result);
        repoMock.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
    }
    
    [Fact]
    public async Task GetUserByIdAsync_Should_Throw_When_Id_Is_Empty()
    {
        var repoMock = new Mock<IUserRepository>();
        var configMock = new Mock<IConfiguration>();
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);
        
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserByIdAsync(Guid.Empty));
    }
    
    [Fact]
    public async Task GetUserByIdAsync_Should_Throw_When_User_Not_Found()
    {
        var userId = Guid.NewGuid();
        
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User)null);
        
        var configMock = new Mock<IConfiguration>();
        var service = new UserService.Service.UserService(repoMock.Object, configMock.Object);
        
        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetUserByIdAsync(userId));
    }
}