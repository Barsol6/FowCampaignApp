namespace FowCampaign.Api.Modules.Database.Entities.User;

public class CampaignPlayer
{
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public virtual Campaign.Campaign Campaign { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; }

    public string FactionName { get; set; } = string.Empty;
    public bool IsAlive { get; set; } = true;
    public bool IsTurn { get; set; } = false;
}