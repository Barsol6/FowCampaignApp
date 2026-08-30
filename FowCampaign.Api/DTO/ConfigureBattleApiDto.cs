namespace FowCampaign.Api.DTO;

public class ConfigureBattleApiDto
{
    public string BattleId { get; set; } = string.Empty;
    public bool IsAmphibious { get; set; }
    public string AttackerFaction { get; set; } = string.Empty;
    public string DefenderFaction { get; set; } = string.Empty;
}
