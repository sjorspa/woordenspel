using Woordenspel.Client.Services;
using Woordenspel.TestSupport;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Services;

public class AuthStateTests
{
    private readonly FakeBrowserStorage _storage = new();
    private readonly FakeTimeProvider _clock = new();

    [Fact]
    public async Task Signed_in_user_survives_a_page_reload()
    {
        await new AuthState(_storage, _clock).SignInAsync(AnnaLogin);

        var reloaded = new AuthState(_storage, _clock);
        await reloaded.InitializeAsync();

        Assert.Equal(AnnaLogin, reloaded.User);
    }

    [Fact]
    public async Task Expired_token_counts_as_logged_out()
    {
        var auth = new AuthState(_storage, _clock);
        await auth.SignInAsync(AnnaLogin);

        _clock.Advance(TimeSpan.FromDays(8));

        Assert.False(auth.IsLoggedIn);
    }

    [Fact]
    public async Task Sign_out_forgets_the_user_and_raises_Changed()
    {
        var auth = new AuthState(_storage, _clock);
        await auth.SignInAsync(AnnaLogin);
        var changes = 0;
        auth.Changed += () => changes++;

        await auth.SignOutAsync();

        Assert.Null(auth.User);
        Assert.Empty(_storage.Items);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Corrupt_storage_is_ignored()
    {
        _storage.Items["woordenspel.auth"] = "{kapot";
        var auth = new AuthState(_storage, _clock);

        await auth.InitializeAsync();

        Assert.False(auth.IsLoggedIn);
    }
}
