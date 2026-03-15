using System.ComponentModel.DataAnnotations.Schema;

namespace FowCampaign.Api.Modules.Database.Entities.Campaign;

public class BattleLog
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    [ForeignKey("CampaignId")] public virtual Campaign Campaign { get; set; }
    public int TurnNumber { get; set; }
    public string ZoneName { get; set; } 
    public string ResultJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
}