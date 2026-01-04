namespace FowCampaign.App.DTO;

public class GameStateDto
{
    public List<FactionDto> Factions { get; set; }
    public List<ZoneSeedDto> Zones { get; set; }
    public List<UnitDto> Units { get; set; } = new();
    public List<UnitDefinitionDto> UnitDefinitions { get; set; } = new();
    public string CurrentTurnFaction { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
}