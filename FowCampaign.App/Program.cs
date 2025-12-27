using FowCampaign.App;
using FowCampaign.App.DTO;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FowCampaign.App.Handlers;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CookieHandler>();

var apiUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddHttpClient("API", client => 
    {
        client.BaseAddress = new Uri(apiUrl);
    })
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));
builder.Services.AddSingleton<User>();
builder.Services.AddScoped<LoginDto>();
builder.Services.AddScoped<RegisterDto>();





var host = builder.Build();

await host.RunAsync();