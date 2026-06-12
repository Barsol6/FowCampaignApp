namespace FowCampaign.Api.DTO;

public class UnitDefinitionApiDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
}