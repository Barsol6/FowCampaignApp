using FowCampaign.Api.DTO;
using FowCampaign.Api.Modules.Database;
using FowCampaign.Api.Modules.Database.Entities.Campaign;
using FowCampaign.Api.Modules.Database.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FowCampaign.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CampaignController:ControllerBase
{
    private readonly FowCampaignContext _context;
    
    public CampaignController(FowCampaignContext context)
    {
        _context = context;
    }

  

   [HttpPost]
   public async Task<IActionResult> CreateCampaign([FromForm] CreateCampaignDto request)
   {
       var nameClaim = User.Identity?.Name;
       if (string.IsNullOrEmpty(nameClaim))
       {
           return Unauthorized();
       }

       var user = _context.Users.FirstOrDefault(u => u.Username == nameClaim);
       if (user is null) return NotFound();

       if (request.MapImage is null || request.MapImage.Length == 0)
       {
           return BadRequest("Map iamge is required");
       }

       if (string.IsNullOrEmpty(request.CreatorFactionName))
       {
           return BadRequest("You must select a faction to play.");
       }
       
       var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.MapImage.FileName)}";
       var mapsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "maps");
       Directory.CreateDirectory(mapsFolder);
       
       var savePath = Path.Combine(mapsFolder, fileName);
       using (var stream = new FileStream(savePath, FileMode.Create))
       {
           await request.MapImage.CopyToAsync(stream);
       }
       
       var joinCode = Path.GetRandomFileName().Replace(".", "").Substring(0, 6).ToUpper();

       var campaign = new Campaign
       {
           Name = request.Name,
           JoinCode = joinCode,
           MapFileName = fileName,
           GameStateJson = request.GameStateJson,
           OwnerId = user.Id
       };

       var player = new CampaignPlayer
       {
           User = user,
           FactionName = request.CreatorFactionName,
           IsAlive = true,
           IsTurn = true
       };
       
       campaign.Players.Add(player);
       
       _context.Campaigns.Add(campaign);
       await _context.SaveChangesAsync();
       
       return Ok(new {message = "Campaign Deployed", joinCode = joinCode });
   }
   
   
   [HttpGet]
   public async Task<IActionResult> GetCampaigns()
   {
     var nameClaim = User.Identity?.Name;
     if (string.IsNullOrEmpty(nameClaim))
     {
         return Unauthorized();
     }
     var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameClaim);
     if (user is null) return NotFound();
     
     var campaigns = await _context.Campaigns
         .Include(c => c.Players)
         .Where(c => c.Players.Any(p => p.UserId == user.Id))
         .OrderByDescending(c=>c.CreatedAt)
         .Select(c=> new CampaignDto
         {
             Id = c.Id,
             Name = c.Name,
             JoinCode = (c.OwnerId == user.Id) ? c.JoinCode : "HIDDEN",
             LastPlayed = c.CreatedAt,
             Status = "ACTIVE"
         }).ToListAsync();
     return Ok(campaigns);
   }
   

   
}
    
