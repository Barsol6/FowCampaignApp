using FowCampaignApp.Data;
using Microsoft.EntityFrameworkCore;


namespace FowCampaignApp.Services.Auth;

public class AuthService
{
    private readonly IDbContextFactory<CampaignContext> _contextFactory;
    
    public AuthService(IDbContextFactory<CampaignContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Player?> LoginAsync(string username, string passwordHash)
    {
        using var context = _contextFactory.CreateDbContext();
        
        return context.Players.FirstOrDefault(p => p.Username == username && p.PasswordHash == passwordHash);
    }
    
    public async Task<Player?> Register(string username, string password, string color)
    {
        using var context = _contextFactory.CreateDbContext();
        
        if ( await context.Players.AnyAsync(p => p.Username == username)) throw new Exception("User already exists!");
        
        var player = new Player
        {
            Username = username, 
            PasswordHash = password, 
            Color = color
        };
        
        context.Players.Add(player);
        await context.SaveChangesAsync();
        
        return player;
    }
    
}