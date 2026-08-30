using FowCampaign.Api.DTO;

namespace FowCampaign.Api.DTO;

public class GameStateDto
{
    public List<FactionApiDto> Factions { get; set; } = new();
    public List<ZoneSeedApiDto> Zones { get; set; } = new();
    public List<UnitApiDto> Units { get; set; } = new();
    public List<UnitDefinitionApiDto> UnitDefinitions { get; set; } = new();
    public string CurrentTurnFaction { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public TurnPhase Phase { get; set; } = TurnPhase.Moving;
    public Dictionary<string, List<UnitManeuver>> PendingManeuvers { get; set; } = new();
    public Dictionary<string, List<UnitManeuver>> MovementDrafts { get; set; } = new();
    public List<string> ConfirmedMovementFactions { get; set; } = new();
    public Dictionary<string, List<string>> AdjacencyGraph { get; set; } = new();
    public List<BattleResultApiDto> BattleResults { get; set; } = new();
    public List<ActiveBattleApiDto> ActiveBattles { get; set; } = new();
    public Dictionary<string, Dictionary<string, BattleStance>> PendingStances { get; set; } = new();
}

public enum TurnPhase
{
    Moving,
    Combat,
    Coloring,
    PostCombatMoving
}
