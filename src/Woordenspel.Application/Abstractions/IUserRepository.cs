using Woordenspel.Domain.Users;

namespace Woordenspel.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Zoekt hoofdletterongevoelig op gebruikersnaam.</summary>
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <exception cref="UsernameTakenException">Als de gebruikersnaam al bestaat.</exception>
    Task AddAsync(User user, CancellationToken cancellationToken);
}
