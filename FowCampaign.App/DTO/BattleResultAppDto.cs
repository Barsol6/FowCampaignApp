namespace FowCampaign.App.DTO;

public class BattleResultAppDto
{
    public string ZoneName { get; set; }
    public int TurnNumber { get; set; }
    public BattleStance Stance { get; set; }
    public Dictionary<string, int> MajorPoints { get; set; } = new();
    public Dictionary<string, int> MinorPoints { get; set; } = new();
    public Dictionary<string, string> UpdatedUnitFiles { get; set; } = new();
}