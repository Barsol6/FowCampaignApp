using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FowCampaignApp;
using SqliteWasmBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSqliteWasmDbContextFactory<AppContext>(
    opts => opts.UseSqlite("Data Source=fow_campaign.db"));

var host = builder.Build();

await host.Services.InitializeSqliteWasmDatabaseAsync<CampaignContext>();

await host.RunAsync();