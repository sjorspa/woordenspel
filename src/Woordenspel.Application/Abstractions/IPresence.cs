namespace Woordenspel.Application.Abstractions;

/// <summary>Wie er op dit moment verbonden is.</summary>
public interface IPresence
{
    bool IsOnline(Guid userId);
}
