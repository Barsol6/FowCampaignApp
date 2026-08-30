namespace FowCampaign.Api.DTO;

public class SubmitStanceApiDto
{
    public string ZoneName { get; set; }
    public BattleStance Stance { get; set; }
    public List<string> FactionsInvolved { get; set; } 
}