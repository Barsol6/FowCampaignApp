namespace FowCampaign.App.DTO;

public class CreateCampaignAppDto
{
    public string Name { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public List<FactionAppDto> Factions { get; set; } = new();
}
