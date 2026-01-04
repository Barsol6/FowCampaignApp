using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FowCampaign.App.DTO;
using Microsoft.AspNetCore.Components.Authorization;

namespace FowCampaign.App.Providers;

public class CustomAuthStateProvider:AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public CustomAuthStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/User/me");

            if (response.IsSuccessStatusCode)
            {
                var userDto = await response.Content.ReadFromJsonAsync<UserSessionDto>(_jsonOptions);
                if (userDto != null && userDto.IsAuthenticated)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userDto.Username)
                    };
                
                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var user = new ClaimsPrincipal(identity);

                    return new AuthenticationState(user);
                }
            
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}