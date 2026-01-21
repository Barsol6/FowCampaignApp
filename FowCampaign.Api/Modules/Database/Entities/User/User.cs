#region

using FowCampaign.Api.Modules.Database.Entities.Map;

#endregion

namespace FowCampaign.Api.Modules.Database.Entities.User;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }


    public virtual ICollection<Territory> Territories { get; set; } = new List<Territory>();

    public virtual ICollection<CampaignPlayer> CampaignsPlayed { get; set; } = new List<CampaignPlayer>();
}