namespace FowCampaign.App.DTO;

public class LoadGameDataDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MapImageBase64 { get; set; } = string.Empty;
    public string GameStateJson { get; set; } = string.Empty;
    public string MyFactionName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
}