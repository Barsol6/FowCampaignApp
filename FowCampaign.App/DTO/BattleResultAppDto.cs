namespace FowCampaign.App.DTO;

public class BattleResultAppDto
{
    public string BattleId { get; set; } = string.Empty;
    public string ZoneName { get; set; }
    public int TurnNumber { get; set; }
    public Dictionary<string, BattleStance> Stances { get; set; } = new();
    public Dictionary<string, int> MajorPoints { get; set; } = new();
    public Dictionary<string, int> MinorPoints { get; set; } = new();
    public Dictionary<string, string> UpdatedUnitFiles { get; set; } = new();
    public string ScenarioName { get; set; } = string.Empty;
    public bool IsAmphibious { get; set; }
    public string AttackerFaction { get; set; } = string.Empty;
    public string DefenderFaction { get; set; } = string.Empty;
    public string CommanderUsername { get; set; } = string.Empty;
    public List<string> WinnerFactions { get; set; } = new();
}
