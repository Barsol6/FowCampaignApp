using System.ComponentModel.DataAnnotations.Schema;

namespace FowCampaign.Api.Modules.Database.Entities.Map;

public class Territory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string BoundaryJson { get; set; }

    public int? OwnerId { get; set; }

    [ForeignKey("OwnerId")] public virtual User.User? Owner { get; set; }

    public int MapId { get; set; }

    [ForeignKey("MapId")] public virtual Map? Map { get; set; }
}