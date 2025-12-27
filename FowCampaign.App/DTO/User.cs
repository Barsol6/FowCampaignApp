namespace FowCampaign.App.DTO;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Color { get; set; }
    
    public string Role { get; set; } = "Player";
}