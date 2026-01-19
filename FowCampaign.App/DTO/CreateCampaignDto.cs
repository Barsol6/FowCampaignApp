namespace FowCampaign.App.DTO;

public class CreateCampaignDto
{
    public string Name { get; set; } = string.Empty;
    public List<FactionDto> Factions { get; set; } = new();
}