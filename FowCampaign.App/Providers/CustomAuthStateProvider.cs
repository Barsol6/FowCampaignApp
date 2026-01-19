using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FowCampaign.App.DTO;
using Microsoft.AspNetCore.Components.Authorization;

namespace FowCampaign.App.Providers;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private AuthenticationState? _cachedAuthenticationState;
    public CustomAuthStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedAuthenticationState != null)
        {
            return _cachedAuthenticationState;
        }
        
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
                        new(ClaimTypes.Name, userDto.Username)
                    };

                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var user = new ClaimsPrincipal(identity);
                    
                    _cachedAuthenticationState = new AuthenticationState(user);

                    return _cachedAuthenticationState;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        _cachedAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        return _cachedAuthenticationState;
    }

    public void NotifyAuthState()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
    
    public void NotifyLogOut()
    {
        _cachedAuthenticationState = null;

        var anonymousState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        
        NotifyAuthenticationStateChanged(Task.FromResult(anonymousState));
    }
}