namespace FowCampaign.App.DTO;

public class MovementArrowAppDto
{
    public string UnitId { get; set; } = string.Empty;
    public string FactionName { get; set; } = string.Empty;
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public bool IsDraft { get; set; }
}
