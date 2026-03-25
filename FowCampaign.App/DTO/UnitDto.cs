namespace FowCampaign.App.DTO;

public class UnitDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DefinitionId { get; set; }
    public string FactionName { get; set; }
    public double X { get; set; }
    public double Y { get; set; }

    public string ExcelFileName { get; set; } = string.Empty;
    public string ExcelDatabase64 { get; set; } = string.Empty;
}