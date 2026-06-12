namespace FowCampaign.App.DTO;

public class GameStateAppDto
{
    public List<FactionAppDto> Factions { get; set; } = new();
    public List<ZoneSeedAppDto> Zones { get; set; } = new();
    public List<UnitAppDto> Units { get; set; } = new();
    public List<UnitDefinitionAppDto> UnitDefinitions { get; set; } = new();
    public string CurrentTurnFaction { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public TurnPhase Phase { get; set; } = TurnPhase.Moving;
    public Dictionary<string, List<UnitManeuver>> PendingManeuvers { get; set; } = new();
    public List<BattleResultAppDto> BattleResults { get; set; } = new();
    public Dictionary<string, List<string>> AdjacencyGraph { get; set; } = new();
    public Dictionary<string, string> PendingStances { get; set; } = new();
}

public enum TurnPhase
{
    Moving,
    Resolution,
    Combat
}