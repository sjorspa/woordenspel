using Woordenspel.Domain.Users;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Register_trims_and_normalizes_the_username()
    {
        var id = Guid.NewGuid();

        var user = User.Register(id, "  Anna_01 ", "hash", TestGames.Now);

        Assert.Equal(id, user.Id);
        Assert.Equal("Anna_01", user.Username);
        Assert.Equal("ANNA_01", user.NormalizedUsername);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(TestGames.Now, user.CreatedAt);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("eenveelstelangenaam123")]
    [InlineData("met spatie")]
    [InlineData("anna@mail")]
    [InlineData("")]
    public void Register_rejects_invalid_usernames(string username)
    {
        var exception = Assert.Throws<DomainException>(() => User.Register(Guid.NewGuid(), username, "hash", TestGames.Now));

        Assert.Equal(DomainErrors.InvalidUsername, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void ValidatePassword_rejects_short_passwords(string password)
    {
        var exception = Assert.Throws<DomainException>(() => User.ValidatePassword(password));

        Assert.Equal(DomainErrors.InvalidPassword, exception.Code);
    }

    [Fact]
    public void ValidatePassword_accepts_eight_characters()
    {
        User.ValidatePassword("12345678");
    }
}
