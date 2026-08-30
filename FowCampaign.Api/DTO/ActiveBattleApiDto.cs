namespace FowCampaign.Api.DTO;

public class ActiveBattleApiDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ZoneName { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public List<string> Factions { get; set; } = new();
    public List<string> UnitIds { get; set; } = new();
    public List<string> InterceptedUnitIds { get; set; } = new();
    public bool IsAmphibious { get; set; }
    public string AttackerFaction { get; set; } = string.Empty;
    public string DefenderFaction { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public List<string> WinnerFactions { get; set; } = new();
    public string ScenarioName { get; set; } = string.Empty;
    public string CommanderUsername { get; set; } = string.Empty;
    public bool ColoringResolved { get; set; }
    public bool ColoringSkipped { get; set; }
    public string ColoringFactionName { get; set; } = string.Empty;
}
