namespace FowCampaign.Api.Modules.Map;

public class Map
{
    public int Id { get; set; }
    public string ImageBase64 { get; set; }
    public string MapName { get; set; }
    
    public virtual ICollection<Territory> Territories { get; set; } = new List<Territory>();
}