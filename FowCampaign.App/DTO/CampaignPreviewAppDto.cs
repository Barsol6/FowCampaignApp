using System.Text.Json.Serialization;

namespace FowCampaign.App.DTO;

public class CampaignPreviewAppDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("factions")]
    public List<string> Factions { get; set; } = new();
}