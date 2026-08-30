namespace FowCampaign.App.DTO;

public class ConfigureBattleAppDto
{
    public string BattleId { get; set; } = string.Empty;
    public bool IsAmphibious { get; set; }
    public string AttackerFaction { get; set; } = string.Empty;
    public string DefenderFaction { get; set; } = string.Empty;
}
