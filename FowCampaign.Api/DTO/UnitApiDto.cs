namespace FowCampaign.Api.DTO;

public class UnitApiDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DefinitionId { get; set; }
    public string FactionName { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string CurrentZoneName { get; set; } = string.Empty;
    public double Scale { get; set; } = 1.0;

    public string ExcelFileName { get; set; } = string.Empty;
    public string ExcelDatabase64 { get; set; } = string.Empty;
}
