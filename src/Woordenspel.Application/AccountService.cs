using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Users;

namespace Woordenspel.Application;

public sealed class AccountService(IUserRepository users, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
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

    public async Task<UserDto> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByUsernameAsync(username ?? string.Empty, cancellationToken);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, password ?? string.Empty))
        {
            throw new AuthenticationFailedException();
        }

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Username);
}
