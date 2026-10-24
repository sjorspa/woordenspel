using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;
using Woordenspel.Infrastructure.Persistence;
using Woordenspel.Infrastructure.Security;
using Woordenspel.Infrastructure.Words;

namespace Woordenspel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WordListOptions>(configuration.GetSection(WordListOptions.SectionName));
        services.AddSingleton<IWordDictionary>(sp =>
            WordList.FromFile(sp.GetRequiredService<IOptions<WordListOptions>>().Value.ResolvePath()));

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddDbContext<WoordenspelDbContext>((sp, options) =>
            options.UseSqlite($"Data Source={sp.GetRequiredService<IOptions<StorageOptions>>().Value.DatabasePath}"));

        services.AddScoped<IGameRepository, EfGameRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<GameSnapshotSerializer>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRandomizer, SystemRandomizer>();
        return services;
    }

    /// <summary>Maakt de datamap aan en brengt het databaseschema bij.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(services.GetRequiredService<IOptions<StorageOptions>>().Value.ResolveDataDirectory());

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoordenspelDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
