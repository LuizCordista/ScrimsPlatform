using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Moq;
using UserService.CustomException;
using UserService.Model;
using UserService.Repository;
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

        var service = new UserService.Service.UserService(repoMock.Object);

        var result = await service.CreateUserAsync(user);

        Assert.Equal(user, result);
        repoMock.Verify(r => r.CreateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_Should_Throw_When_Username_Or_Email_Is_Empty()
    {
        var user = new User("", "", "123");
        var repoMock = new Mock<IUserRepository>();
        var service = new UserService.Service.UserService(repoMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateUserAsync(user));
    }

    [Fact]
    public async Task CreateUserAsync_Should_Throw_When_Username_Exists()
    {
        var user = new User("testuser", "test@email.com", "123");
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync(user.Username))
            .ReturnsAsync(new User("existinguser", "existing@email.com", "pass"));
        var service = new UserService.Service.UserService(repoMock.Object);

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
    }
}