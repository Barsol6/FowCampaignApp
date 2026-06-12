using FowCampaign.App.DTO;

namespace FowCampaign.Api.DTO;

public class EndTurnRequestApiDto
{
    public List<UnitApiDto> Units { get; set; } = new();
    public List<ZoneSeedApiDto> Zones { get; set; } = new();
}