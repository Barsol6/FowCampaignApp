namespace FowCampaign.Api.DTO;

public class CampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime LastPlayed { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string JoinCode { get; set; } = string.Empty; 
}