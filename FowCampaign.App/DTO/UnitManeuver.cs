namespace FowCampaign.App.DTO;

public class UnitManeuver
{
    public string UnitId { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public string OriginZoneName { get; set; } = string.Empty;
    public string IntermediateZoneName { get; set; } = string.Empty;
    public string DestinationZoneName { get; set; } = string.Empty;
}
