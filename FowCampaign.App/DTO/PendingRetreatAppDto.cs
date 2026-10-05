namespace FowCampaign.App.DTO;

public class PendingRetreatAppDto
{
    public string UnitId { get; set; } = string.Empty;
    public List<string> AllowedZoneNames { get; set; } = new();
    public bool MustReturnToOrigin { get; set; }
}
