namespace FowCampaign.App.DTO;

public class UnitDefinitionDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
}