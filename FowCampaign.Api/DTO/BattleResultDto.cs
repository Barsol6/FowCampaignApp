namespace FowCampaign.Api.DTO;

public class BattleResultDto
{
    public string ZoneName { get; set; }
    public int TurnNumber { get; set; }
    
    public Dictionary<string, int> MajorPoints { get; set; } = new();
    public Dictionary<string, int> MinorPoints { get; set; } = new();

    public List<string> EliminatedUnitInfo { get; set; } = new();
}