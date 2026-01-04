using System.Text.Json;
using FowCampaign.Api.DTO;
using FowCampaign.Api.Modules.Database;
using FowCampaign.Api.Modules.Database.Entities.Campaign;
using FowCampaign.Api.Modules.Database.Entities.User;
using FowCampaign.App.DTO;
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
           return BadRequest("Map image is required");
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

   [HttpGet("{id}")]
   [Authorize]
   public async Task<ActionResult<LoadGameDataDto>> GetCampaign(int id)
   {
       var username = User.Identity?.Name;
       var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
       if (user == null)
       {
           return Unauthorized();
       }
       
       var campaign = await _context.Campaigns.Include(c=> c.Players)
           .FirstOrDefaultAsync(c => c.Id == id);

       if (campaign == null)
       {
           return NotFound("Campaign Not Found");
       }

       var playerRecord = campaign.Players.FirstOrDefault(p => p.UserId == user.Id);
       if (playerRecord == null)
       {
           return Unauthorized("You are not a member of this campaign");
       }

       string base64Map = "";
       if (!string.IsNullOrEmpty(campaign.MapFileName))
       {
           try
           {
               var mapPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "maps", campaign.MapFileName);
               if (System.IO.File.Exists(mapPath))
               {
                   var bytes = await System.IO.File.ReadAllBytesAsync(mapPath);
                   base64Map = "data:image/png;base64," + Convert.ToBase64String(bytes);
               }
               else
               {
                   Console.WriteLine($"Map file not found: {mapPath}");
               }
           }catch(Exception ex)
           {
               Console.WriteLine($"Error reading map file: {ex.Message}");
           }
       }

       return Ok(new LoadGameDataDto
       {
            Id = campaign.Id,
            Name = campaign.Name,
            GameStateJson = campaign.GameStateJson,
            MapImageBase64 = base64Map,
            MyFactionName = playerRecord.FactionName,
            IsHost = (campaign.OwnerId == user.Id)
       });


   }

   [HttpPost("{Id}/turn")]
   [Authorize]
   public async Task<IActionResult> EndTurn(int id, [FromBody] List<UnitDto> updatedUnits)
   {
       var username = User.Identity?.Name;
       var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
       if (user == null)
       {
           return Unauthorized();
       }
       
       var campaign = await _context.Campaigns.Include(c=> c.Players)
           .FirstOrDefaultAsync(c => c.Id == id);
       if (campaign == null)
       {
           return NotFound("Campaign Not Found");
       }
       
       var playerRecord = campaign.Players.FirstOrDefault(p => p.UserId == user.Id);
       if (playerRecord == null)
       {
           return Unauthorized("You are not a member of this campaign");
       }
       
       var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson);
       if (state == null)
       {
           return BadRequest("Invalid game state");
       }

       if (state.CurrentTurnFaction != playerRecord.FactionName)
       {
           return BadRequest($"It is not {playerRecord.FactionName}'s turn! Current turn: {state.CurrentTurnFaction}");
       }

       state.Units = updatedUnits;
       
       var currentFactionIndex = state.Factions.FindIndex(f => f.Name == state.CurrentTurnFaction);
       
       var nextFactionIndex = (currentFactionIndex + 1) % state.Factions.Count;

       state.CurrentTurnFaction = state.Factions[nextFactionIndex].Name;

       if (nextFactionIndex == 0) 
       {
           state.TurnNumber++;
       }
       
       campaign.GameStateJson = JsonSerializer.Serialize(state);
       campaign.CreatedAt = DateTime.UtcNow;
       await _context.SaveChangesAsync();
       
       return Ok(new
       {
           message = "Turn ended",
           nextFaction = state.CurrentTurnFaction,
           turnNumber = state.TurnNumber
       });
   }

   [HttpPost("join")]
   [Authorize]
   public async Task<IActionResult> JoinCampaign([FromBody] JoinRequestDto joinRequestDto)
   {
       var username = User.Identity?.Name;
       var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
       if (user == null)
       {
           return Unauthorized();
       }
       
       var code = joinRequestDto.JoinCode.ToUpper().Trim();
       var campaign = await _context.Campaigns
           .Include(c => c.Players)
           .FirstOrDefaultAsync(c => c.JoinCode == code);
       if (campaign == null)
       {
           return NotFound("Campaign Not Found");
       }

       if (campaign.Players.Any(p => p.UserId == user.Id))
       {
           return Ok(new { campaignId = campaign.Id, message = "Welcome back, Commander." });
       }
       
       var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson);
       if (state == null)
       {
           return BadRequest("Invalid game state");
       }

       var targetFactionName = "";
       
       campaign.Players.Add(new CampaignPlayer
       {
           User = user,
           FactionName = joinRequestDto.FactionName,
           IsAlive = true,
           IsTurn = true
       });
       await _context.SaveChangesAsync();
       return Ok(new { campaignId = campaign.Id, message = "Welcome to the campaign, Commander." });
   }
   
   [HttpGet("lookup/{code}")]
   [Authorize]
   public async Task<IActionResult> LookupCampaign(string code)
   {
       var username = User.Identity?.Name;
       var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
       if (user == null)
       {
           return Unauthorized();
       }
       
       var cleanCode = code.ToUpper().Trim();
       var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.JoinCode == cleanCode);

       if (campaign == null) return NotFound("Unknown Operation Code.");


       var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson);
       var factionNames = state?.Factions.Select(f => f.Name).ToList() ?? new List<string>();

       return Ok(new 
       { 
           Name = campaign.Name, 
           Factions = factionNames 
       });
   }
   
}
    
