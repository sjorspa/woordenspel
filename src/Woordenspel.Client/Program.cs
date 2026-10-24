using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Woordenspel.Client;
using Woordenspel.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddWoordenspelClient(new Uri(builder.HostEnvironment.BaseAddress));

var host = builder.Build();
await host.Services.GetRequiredService<AuthState>().InitializeAsync();
await host.RunAsync();
