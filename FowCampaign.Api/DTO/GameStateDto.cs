using FowCampaign.App.DTO;

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
}

public enum TurnPhase
{
    Moving,
    Resolution,
    Combat
}