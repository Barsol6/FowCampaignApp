using FowCampaign.Api.Modules.Database.Entities.User;

namespace FowCampaign.Api.Modules.Database.Entities.Campaign;

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string JoinCode { get; set; } = string.Empty;
    public string MapFileName { get; set; } = string.Empty;
    public string GameStateJson { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public virtual ICollection<CampaignPlayer> Players { get; set; } = new List<CampaignPlayer>();
    public DateTime CreatedAt { get; set; }

}