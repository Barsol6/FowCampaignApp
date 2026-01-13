using FowCampaign.Api.Modules.Database.Entities.Campaign;
using FowCampaign.Api.Modules.Database.Entities.Map;
using FowCampaign.Api.Modules.Database.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace FowCampaign.Api.Modules.Database;

public class FowCampaignContext : DbContext
{
    public FowCampaignContext(DbContextOptions<FowCampaignContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Territory> Territories { get; set; }
    public DbSet<Map> MapConfigs { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Territory>(entity =>
        {
            entity.HasOne(x => x.Map)
                .WithMany(x => x.Territories)
                .HasForeignKey(x => x.MapId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Owner)
                .WithMany(x => x.Territories)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CampaignPlayer>(entity =>
        {
            entity.HasOne(cp => cp.Campaign)
                .WithMany(c => c.Players)
                .HasForeignKey(cp => cp.CampaignId);

            entity.HasOne(cp => cp.User)
                .WithMany(u => u.CampaignsPlayed)
                .HasForeignKey(cp => cp.UserId);
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Map>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MapName).IsRequired().HasMaxLength(50);
        });

        base.OnModelCreating(modelBuilder);
    }
}