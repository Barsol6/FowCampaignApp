namespace FowCampaign.Api.DTO;

public class MoveRetreatApiDto
{
    public string UnitId { get; set; } = string.Empty;
    public string DestinationZoneName { get; set; } = string.Empty;
    public double TargetX { get; set; }
    public double TargetY { get; set; }
}
