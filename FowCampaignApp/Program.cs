using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FowCampaignApp;
using FowCampaignApp.Data;
using FowCampaignApp.Services.Auth;
using SqliteWasmBlazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });



builder.Services.AddDbContextFactory<CampaignContext>(opts =>
{
    var connection = new SqliteWasmConnection("Data Source=fow_campaign.db");
    opts.UseSqliteWasm(connection);
});

builder.Services.AddSingleton<IDBInitializationService, DBInitializationService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<SyncService>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
    await sync.SyncOnStartup();
}
await host.Services.InitializeSqliteWasmDatabaseAsync<CampaignContext>();

await host.RunAsync();