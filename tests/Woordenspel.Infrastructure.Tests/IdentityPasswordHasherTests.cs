using Woordenspel.Infrastructure.Security;

namespace Woordenspel.Infrastructure.Tests;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_can_be_verified_with_the_same_password_only()
    {
        var hash = _hasher.Hash("geheim123");

        Assert.DoesNotContain("geheim123", hash);
        Assert.True(_hasher.Verify(hash, "geheim123"));
        Assert.False(_hasher.Verify(hash, "Geheim123"));
    }

    [Fact]
    public void Same_password_gives_different_hashes()
    {
        Assert.NotEqual(_hasher.Hash("geheim123"), _hasher.Hash("geheim123"));
    }
}
