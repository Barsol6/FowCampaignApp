namespace FowCampaign.Api.DTO;

public class CreateUnitApiDto
{
    public string DefinitionId { get; set; } = string.Empty;
    public string NewDefinitionName { get; set; } = string.Empty;
    public string NewDefinitionImageBase64 { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string CurrentZoneName { get; set; } = string.Empty;
}
