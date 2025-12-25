using Microsoft.EntityFrameworkCore;

namespace FowCampaignApp.Data;

public class CampaignContext : DbContext
{
    public CampaignContext(DbContextOptions<CampaignContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Territory> Territories { get; set; }
    public DbSet<MapConfig> MapConfigs { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Color { get; set; }
    
    public string Role { get; set; } = "Player";
}

public class Territory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public string BoundaryJson { get; set; }
}

public class MapConfig
{
    public int Id { get; set; }
    public string ImageBase64 { get; set; }
    public string MapName { get; set; }
}