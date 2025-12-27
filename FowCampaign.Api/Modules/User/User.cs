using FowCampaign.Api.Modules.Map;

namespace FowCampaign.Api.Modules.User;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public string Color { get; set; }
    
    public string Role { get; set; } = "Player";
    
    public virtual ICollection<Territory> Territories { get; set; } = new List<Territory>();
}