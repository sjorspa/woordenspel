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

    [Fact]
    public void Registered_user_is_not_a_guest()
    {
        Assert.False(User.Register(Guid.NewGuid(), "Anna", "hash", TestGames.Now).IsGuest);
    }

    [Fact]
    public void Register_requires_a_password_hash()
    {
        Assert.Throws<ArgumentException>(() => User.Register(Guid.NewGuid(), "Anna", "", TestGames.Now));
    }

    [Fact]
    public void Guest_has_no_password()
    {
        var guest = User.RegisterGuest(Guid.NewGuid(), "SnelleVos42", TestGames.Now);

        Assert.True(guest.IsGuest);
        Assert.Null(guest.PasswordHash);
        Assert.Equal("SNELLEVOS42", guest.NormalizedUsername);
    }

    [Fact]
    public void Guest_name_follows_the_username_rules()
    {
        var exception = Assert.Throws<DomainException>(() => User.RegisterGuest(Guid.NewGuid(), "a b", TestGames.Now));

        Assert.Equal(DomainErrors.InvalidUsername, exception.Code);
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
