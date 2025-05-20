using JetBrains.Annotations;
using UserService.Service;
using Xunit;

namespace UserService.Tests.Service;

[TestSubject(typeof(PasswordHasher))]
public class PasswordHasherTest
{
    [Fact]
    public void HashPassword_And_VerifyPassword_Should_Work_Correctly()
    {
        var passwordHasher = new PasswordHasher();
        var password = "TestPassword123!";

        var hashedPassword = passwordHasher.HashPassword(password);

        Assert.True(passwordHasher.verifyPassword(password, hashedPassword));
        Assert.False(passwordHasher.verifyPassword("WrongPassword", hashedPassword));
    }
}