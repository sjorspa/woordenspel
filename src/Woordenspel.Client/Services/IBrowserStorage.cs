using Microsoft.JSInterop;

namespace Woordenspel.Client.Services;

/// <summary>Opslag in de browser die bewaard blijft na het sluiten van het tabblad.</summary>
public interface IBrowserStorage
{
    ValueTask<string?> GetAsync(string key);

    ValueTask SetAsync(string key, string value);

    ValueTask RemoveAsync(string key);
}

public sealed class LocalBrowserStorage(IJSRuntime js) : IBrowserStorage
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveAsync(string key) => js.InvokeVoidAsync("localStorage.removeItem", key);
}
