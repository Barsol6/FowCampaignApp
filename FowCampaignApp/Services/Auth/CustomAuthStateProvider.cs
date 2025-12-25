using FowCampaignApp.Data;

namespace FowCampaignApp.Services.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public class CustomAuthStateProvider:AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public void NotifyUserLoggedIn(Player player)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
            new Claim(ClaimTypes.Name, player.Username),
            new Claim("Color", player.Color),
            new Claim(ClaimTypes.Role, player.Role)
        }, "LocalAuth");

        _currentUser = new ClaimsPrincipal(identity);
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
    
    public void NotifyUserLoggedOut()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}