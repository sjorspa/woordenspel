using Woordenspel.Contracts;
using Woordenspel.Domain;
using Woordenspel.TestSupport;

namespace Woordenspel.Application.Tests;

public class AccountServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly AccountService _accounts;

    public AccountServiceTests()
    {
        // Gastnamen: index 0 en 1 in de lijsten, dan nummer 7 -> "BlijeBever07" ...
        _accounts = CreateService(new SequenceRandomizer(0, 1, 7));
    }

    private AccountService CreateService(SequenceRandomizer randomizer) =>
        new(_users, new FakePasswordHasher(), new GuestNameGenerator(randomizer), new FakeTimeProvider());

    [Fact]
    public async Task Register_stores_the_user_with_a_hashed_password()
    {
        var result = await _accounts.RegisterAsync("Anna", "geheim123");

        var user = Assert.Single(_users.Users);
        Assert.Equal(new UserDto(user.Id, "Anna", IsGuest: false), result);
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

    [Fact]
    public async Task Guest_gets_a_random_name_and_no_password()
    {
        var guest = await _accounts.CreateGuestAsync();

        Assert.Equal(new UserDto(guest.Id, "BlijeBever07", IsGuest: true), guest);
        var stored = Assert.Single(_users.Users);
        Assert.Null(stored.PasswordHash);
    }

    [Fact]
    public async Task Guest_name_that_is_taken_is_skipped()
    {
        _users.Add("BlijeBever07");
        var accounts = CreateService(new SequenceRandomizer(0, 1, 7, 2, 3, 99));

        var guest = await accounts.CreateGuestAsync();

        Assert.Equal("GekkeDolfijn99", guest.Username);
    }

    [Fact]
    public async Task Guest_cannot_log_in_with_any_password()
    {
        var guest = await _accounts.CreateGuestAsync();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _accounts.AuthenticateAsync(guest.Username, ""));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _accounts.AuthenticateAsync(guest.Username, "geheim123"));
    }

    [Fact]
    public async Task Gives_up_when_no_free_guest_name_is_found()
    {
        _users.Add("BlijeBever07");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _accounts.CreateGuestAsync());
    }
}

public class GuestNameGeneratorTests
{
    [Fact]
    public void Every_possible_name_is_a_valid_username()
    {
        var longest = GuestNameGenerator.Adjectives.Max(a => a.Length) + GuestNameGenerator.Animals.Max(a => a.Length) + 2;

        Assert.True(longest <= Domain.Users.User.MaxUsernameLength, $"Langste gastnaam heeft {longest} tekens.");
        Assert.All(
            from adjective in GuestNameGenerator.Adjectives
            from animal in GuestNameGenerator.Animals
            select $"{adjective}{animal}00",
            name => Assert.True(Domain.Users.User.IsValidUsername(name), name));
    }

    [Fact]
    public void Name_combines_adjective_animal_and_two_digits()
    {
        Assert.Equal("VrolijkeVos05", new GuestNameGenerator(new SequenceRandomizer(17, 18, 5)).Next());
    }
}
