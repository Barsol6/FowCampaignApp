namespace FowCampaign.App.DTO;

public class EndTurnRequestDto
{
    public List<UnitDto> Units { get; set; } = new();
    public List<ZoneSeedDto> Zones { get; set; } = new();
}