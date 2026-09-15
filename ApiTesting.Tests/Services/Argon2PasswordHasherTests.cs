using ApiTesting.Services;
using Xunit;

namespace ApiTesting.Tests.Services;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword()
    {
        var hash1 = _hasher.HashPassword("correct-horse");
        var hash2 = _hasher.HashPassword("correct-horse");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var hash = _hasher.HashPassword("correct-horse");

        Assert.True(_hasher.VerifyPassword(hash, "correct-horse"));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var hash = _hasher.HashPassword("correct-horse");

        Assert.False(_hasher.VerifyPassword(hash, "wrong-password"));
    }
}
