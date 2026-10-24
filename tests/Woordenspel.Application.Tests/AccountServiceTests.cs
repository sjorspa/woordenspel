using Woordenspel.Domain;
using Woordenspel.TestSupport;

namespace Woordenspel.Application.Tests;

public class AccountServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly AccountService _accounts;

    public AccountServiceTests()
    {
        _accounts = new AccountService(_users, new FakePasswordHasher(), new FakeTimeProvider());
    }

    [Fact]
    public async Task Register_stores_the_user_with_a_hashed_password()
    {
        var result = await _accounts.RegisterAsync("Anna", "geheim123");

        var user = Assert.Single(_users.Users);
        Assert.Equal(new UserDto(user.Id, "Anna"), result);
        Assert.Equal("hashed:geheim123", user.PasswordHash);
        Assert.Equal(FakeTimeProvider.Start, user.CreatedAt);
    }

    [Fact]
    public async Task Register_rejects_a_taken_username_regardless_of_case()
    {
        await _accounts.RegisterAsync("Anna", "geheim123");

        await Assert.ThrowsAsync<UsernameTakenException>(() => _accounts.RegisterAsync("ANNA", "anderwachtwoord"));
    }

    [Theory]
    [InlineData("a", "geheim123", DomainErrors.InvalidUsername)]
    [InlineData("Anna", "kort", DomainErrors.InvalidPassword)]
    public async Task Register_validates_username_and_password(string username, string password, string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _accounts.RegisterAsync(username, password));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Authenticate_accepts_the_right_password_and_any_username_case()
    {
        var registered = await _accounts.RegisterAsync("Anna", "geheim123");

        Assert.Equal(registered, await _accounts.AuthenticateAsync("anna", "geheim123"));
    }

    [Theory]
    [InlineData("Anna", "fout-wachtwoord")]
    [InlineData("Onbekend", "geheim123")]
    public async Task Authenticate_rejects_wrong_credentials(string username, string password)
    {
        await _accounts.RegisterAsync("Anna", "geheim123");

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _accounts.AuthenticateAsync(username, password));
    }
}
