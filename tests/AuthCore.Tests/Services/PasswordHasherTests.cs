using AuthCore.Infrastructure.Services;
using Xunit;

namespace AuthCore.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        var password = "TestPassword123!";
        var hash = _passwordHasher.HashPassword(password);
        
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_SamePlaintext_ShouldProduceDifferentHashes()
    {
        var password = "TestPassword123!";
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);
        
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        var password = "TestPassword123!";
        var hash = _passwordHasher.HashPassword(password);
        
        var result = _passwordHasher.VerifyPassword(password, hash);
        
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(password);
        
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);
        
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("VeryLongPasswordWithSpecialCharacters!@#$%^&*()_+-=[]{}|;:',.<>?")]
    public void HashPassword_VariousLengths_ShouldSucceed(string password)
    {
        var hash = _passwordHasher.HashPassword(password);
        var verified = _passwordHasher.VerifyPassword(password, hash);
        
        Assert.True(verified);
    }
}
