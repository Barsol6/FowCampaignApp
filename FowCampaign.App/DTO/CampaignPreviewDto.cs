namespace FowCampaign.App.DTO;

public class CampaignPreviewDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Factions { get; set; } = new();
}