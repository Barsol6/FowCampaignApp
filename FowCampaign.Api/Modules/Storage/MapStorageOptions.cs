namespace FowCampaign.Api.Modules.Storage;

public sealed class MapStorageOptions
{
    public string Directory { get; set; } = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "maps");
}
