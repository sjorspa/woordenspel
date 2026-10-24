using Woordenspel.Domain.Games;
using Woordenspel.Domain.Seeks;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Seeks;

public class GameSeekTests
{
    private static readonly PlayerIdentity Anna = new(Guid.NewGuid(), "anna");
    private static readonly PlayerIdentity Bram = new(Guid.NewGuid(), "bram");
    private static readonly PlayerIdentity Charlie = new(Guid.NewGuid(), "charlie");

    [Fact]
    public void Anyone_can_join_an_open_game()
    {
        var seek = GameSeek.Open(Guid.NewGuid(), Anna, TestGames.Now);

        Assert.True(seek.IsOpen);
        Assert.Equal([Anna, Bram], seek.Accept(Bram));
        Assert.Equal([Anna, Charlie], seek.Accept(Charlie));
    }

    [Fact]
    public void Only_the_challenged_player_can_accept_a_challenge()
    {
        var seek = GameSeek.Challenge(Guid.NewGuid(), Anna, Bram, TestGames.Now);

        Assert.Equal([Anna, Bram], seek.Accept(Bram));
        var exception = Assert.Throws<DomainException>(() => seek.Accept(Charlie));
        Assert.Equal(DomainErrors.SeekNotForYou, exception.Code);
    }

    [Fact]
    public void You_cannot_join_your_own_game()
    {
        var seek = GameSeek.Open(Guid.NewGuid(), Anna, TestGames.Now);

        var exception = Assert.Throws<DomainException>(() => seek.Accept(Anna));

        Assert.Equal(DomainErrors.DuplicatePlayer, exception.Code);
    }

    [Fact]
    public void You_cannot_challenge_yourself()
    {
        var exception = Assert.Throws<DomainException>(() => GameSeek.Challenge(Guid.NewGuid(), Anna, Anna, TestGames.Now));

        Assert.Equal(DomainErrors.DuplicatePlayer, exception.Code);
    }

    [Fact]
    public void Open_games_are_visible_to_everyone_challenges_only_to_both_players()
    {
        var open = GameSeek.Open(Guid.NewGuid(), Anna, TestGames.Now);
        var challenge = GameSeek.Challenge(Guid.NewGuid(), Anna, Bram, TestGames.Now);

        Assert.True(open.IsVisibleTo(Charlie.Id));
        Assert.True(challenge.IsVisibleTo(Anna.Id));
        Assert.True(challenge.IsVisibleTo(Bram.Id));
        Assert.False(challenge.IsVisibleTo(Charlie.Id));
    }
}
