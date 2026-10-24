using Woordenspel.Application.Abstractions;
using Woordenspel.Contracts;
using Woordenspel.Domain.Users;

namespace Woordenspel.Application;

public sealed class AccountService(
    IUserRepository users, IPasswordHasher passwordHasher, GuestNameGenerator guestNames, TimeProvider timeProvider)
{
    private const int GuestNameAttempts = 20;

    public async Task<UserDto> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        User.ValidatePassword(password);
        if (await users.FindByUsernameAsync(username ?? string.Empty, cancellationToken) is not null)
        {
            throw new UsernameTakenException(username!.Trim());
        }

        var user = User.Register(Guid.NewGuid(), username!, passwordHasher.Hash(password), timeProvider.GetUtcNow());
        await users.AddAsync(user, cancellationToken);
        return ToDto(user);
    }

    /// <summary>Maakt een gast aan met een willekeurige, nog vrije naam.</summary>
    public async Task<UserDto> CreateGuestAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < GuestNameAttempts; attempt++)
        {
            var name = guestNames.Next();
            if (await users.FindByUsernameAsync(name, cancellationToken) is not null)
            {
                continue;
            }

            try
            {
                var guest = User.RegisterGuest(Guid.NewGuid(), name, timeProvider.GetUtcNow());
                await users.AddAsync(guest, cancellationToken);
                return ToDto(guest);
            }
            catch (UsernameTakenException)
            {
                // Net tegelijk door iemand anders gekozen; probeer een andere naam.
            }
        }

        throw new InvalidOperationException("Er is geen vrije gastnaam gevonden.");
    }

    public async Task<UserDto> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByUsernameAsync(username ?? string.Empty, cancellationToken);
        if (user?.PasswordHash is not { } hash || !passwordHasher.Verify(hash, password ?? string.Empty))
        {
            throw new AuthenticationFailedException();
        }

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Username, user.IsGuest);
}
