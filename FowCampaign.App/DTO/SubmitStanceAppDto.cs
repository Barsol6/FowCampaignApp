namespace FowCampaign.App.DTO;

public class SubmitStanceAppDto
{
    public string ZoneName { get; set; }
    public BattleStance Stance { get; set; }
    public List<string> FactionsInvolved { get; set; } = new();
}