using FowCampaign.App;
using FowCampaign.App.DTO;
using FowCampaign.App.Handlers;
using FowCampaign.App.Providers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });



builder.Services.AddTransient<CookieHandler>();

var apiUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddHttpClient("API", client => { client.BaseAddress = new Uri(apiUrl); })
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));
builder.Services.AddSingleton<User>();
builder.Services.AddScoped<LoginDto>();
builder.Services.AddScoped<RegisterDto>();
builder.Services.AddScoped<UserSessionDto>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();


var host = builder.Build();

await host.RunAsync();