using Microsoft.EntityFrameworkCore;
using Woordenspel.Domain.Users;

namespace Woordenspel.Infrastructure.Persistence;

public sealed class WoordenspelDbContext(DbContextOptions<WoordenspelDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<GameRecord> Games => Set<GameRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.Property(u => u.Username).HasMaxLength(User.MaxUsernameLength).IsRequired();
            user.Property(u => u.NormalizedUsername).HasMaxLength(User.MaxUsernameLength).IsRequired();
            user.HasIndex(u => u.NormalizedUsername).IsUnique();
            user.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<GameRecord>(game =>
        {
            game.HasKey(g => g.Id);
            game.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            game.Property(g => g.State).IsRequired();
            game.Property(g => g.Version).IsConcurrencyToken();
            game.HasMany(g => g.Players).WithOne().HasForeignKey(p => p.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GamePlayerRecord>(player =>
        {
            player.ToTable("GamePlayers");
            player.HasKey(p => new { p.GameId, p.UserId });
            player.HasIndex(p => p.UserId);
            player.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
