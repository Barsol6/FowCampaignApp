using FowCampaign.Api.Modules.Map;
using Microsoft.EntityFrameworkCore;

namespace FowCampaign.Api.Modules.Database;

public class FowCampaignContext : DbContext
{
    
    public FowCampaignContext(DbContextOptions<FowCampaignContext> options) : base(options)
    {
    }

    public DbSet<User.User> Users { get; set; }
    public DbSet<Territory> Territories { get; set; }
    public DbSet<Map.Map> MapConfigs { get; set; }

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




        modelBuilder.Entity<User.User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Map.Map>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MapName).IsRequired().HasMaxLength(50);
        });
        
        base.OnModelCreating(modelBuilder);
    }
}





