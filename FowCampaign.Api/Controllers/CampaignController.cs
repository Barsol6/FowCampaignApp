using System.Text.Json;
using FowCampaign.Api.DTO;
using FowCampaign.Api.Modules.Database;
using FowCampaign.Api.Modules.Database.Entities.Campaign;
using FowCampaign.Api.Modules.Database.Entities.User;
using FowCampaign.Api.Modules.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TurnPhase = FowCampaign.Api.DTO.TurnPhase;
using UnitManeuver = FowCampaign.Api.DTO.UnitManeuver;

namespace FowCampaign.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CampaignController : ControllerBase
{
    private readonly FowCampaignContext _context;

    private readonly IHubContext<GameHub> _hubContext;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CampaignController(FowCampaignContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCampaign([FromForm] CreateCampaignApiDto request)
    {
        var nameClaim = User.Identity?.Name;
        if (string.IsNullOrEmpty(nameClaim))
        {
            return Unauthorized();
        }

        var user = _context.Users.FirstOrDefault(u => u.Username == nameClaim);
        if (user is null)
        {
            return NotFound();
        }

        if (request.MapImage.Length == 0)
        {
            return BadRequest("Map image is required");
        }

        if (string.IsNullOrEmpty(request.CreatorFactionName))
        {
            return BadRequest("You must select a faction to play.");
        }

        var initialState = JsonSerializer.Deserialize<GameStateDto>(request.GameStateJson, _jsonOptions);
        if (initialState == null || initialState.Factions.Count == 0)
        {
            return BadRequest("A campaign must contain at least one faction.");
        }

        if (string.IsNullOrWhiteSpace(initialState.Rules))
        {
            return BadRequest("Campaign rules are required.");
        }

        if (!initialState.Factions.Any(f => f.Name == request.CreatorFactionName))
        {
            return BadRequest("The creator must select a faction from this campaign.");
        }

        if (string.IsNullOrWhiteSpace(initialState.CurrentTurnFaction))
        {
            initialState.CurrentTurnFaction = initialState.Factions[0].Name;
        }

        if (initialState.TurnNumber < 1)
        {
            initialState.TurnNumber = 1;
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
            GameStateJson = JsonSerializer.Serialize(initialState, _jsonOptions),
            OwnerId = user.Id,
            CreatedAt = DateTime.UtcNow
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

        return Ok(new { message = "Campaign Deployed", joinCode });
    }

    [HttpGet("GetCampaigns")]
    public async Task<IActionResult> GetCampaigns()
    {
        var nameClaim = User.Identity?.Name;
        if (string.IsNullOrEmpty(nameClaim))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nameClaim);
        if (user is null)
        {
            return NotFound();
        }

        var campaigns = await _context.Campaigns
            .Include(c => c.Players)
            .Where(c => c.Players.Any(p => p.UserId == user.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CampaignApiDto
            {
                Id = c.Id,
                Name = c.Name,
                JoinCode = c.OwnerId == user.Id ? c.JoinCode : "HIDDEN",
                LastPlayed = c.CreatedAt,
                Status = "ACTIVE"
            }).ToListAsync();
        return Ok(campaigns);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<LoadGameDataApiDto>> GetCampaign(int id)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized();
        }

        var campaign = await _context.Campaigns.Include(c => c.Players).ThenInclude(player => player.User)
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

        var base64Map = "";
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading map file: {ex.Message}");
            }
        }

        var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson, _jsonOptions);
        if (state == null)
        {
            return BadRequest("Invalid game state");
        }

        return Ok(new LoadGameDataApiDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            GameStateJson = SerializeStateForFaction(state, playerRecord.FactionName),
            MapImageBase64 = base64Map,
            MyFactionName = playerRecord.FactionName,
            MyUsername = user.Username,
            IsHost = campaign.OwnerId == user.Id,
            PlayersByFaction = campaign.Players
                .GroupBy(player => player.FactionName)
                .ToDictionary(group => group.Key, group => group.Select(player => player.User.Username).ToList())
        });
    }

    [HttpPost("{id}/unit-zones")]
    public async Task<IActionResult> SynchronizeUnitZones(int id, [FromBody] Dictionary<string, string> zoneNamesByUnitId)
    {
        var (campaign, _, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var validZoneNames = state!.Zones.Select(zone => zone.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var changed = false;
        foreach (var unit in state.Units)
        {
            if (!string.IsNullOrWhiteSpace(unit.CurrentZoneName))
            {
                continue;
            }

            if (!zoneNamesByUnitId.TryGetValue(unit.Id, out var zoneName) || !validZoneNames.Contains(zoneName))
            {
                continue;
            }

            unit.CurrentZoneName = zoneName;
            changed = true;
        }

        if (!changed)
        {
            return Ok();
        }

        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/unit/{unitId}/excel")]
    public async Task<IActionResult> UpdateUnitExcel(int id, string unitId, [FromBody] UpdateUnitExcelApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var unit = state!.Units.FirstOrDefault(candidate => candidate.Id == unitId);
        if (unit == null)
        {
            return NotFound("Unit not found.");
        }

        if (!string.Equals(unit.FactionName, player!.FactionName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.ExcelDatabase64))
        {
            return BadRequest("The spreadsheet is empty.");
        }

        try
        {
            Convert.FromBase64String(request.ExcelDatabase64);
        }
        catch (FormatException)
        {
            return BadRequest("The spreadsheet data is invalid.");
        }

        unit.ExcelFileName = request.ExcelFileName;
        unit.ExcelDatabase64 = request.ExcelDatabase64;
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}_Faction_{player.FactionName}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/units")]
    public async Task<IActionResult> CreateUnit(int id, [FromBody] CreateUnitApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        if (state!.Phase is not (TurnPhase.Moving or TurnPhase.PostCombatMoving) ||
            state.ConfirmedMovementFactions.Contains(player!.FactionName))
        {
            return BadRequest("Armies can be added only before your faction confirms movement.");
        }

        if (!double.IsFinite(request.X) || !double.IsFinite(request.Y) || request.X < 0 || request.Y < 0)
        {
            return BadRequest("Select a valid map position.");
        }

        var zone = state.Zones.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, request.CurrentZoneName, StringComparison.OrdinalIgnoreCase));
        if (zone == null)
        {
            return BadRequest("Select a named region for the army.");
        }

        var definition = state.UnitDefinitions.FirstOrDefault(candidate => candidate.Id == request.DefinitionId);
        if (definition == null)
        {
            if (string.IsNullOrWhiteSpace(request.NewDefinitionName) ||
                request.NewDefinitionImageBase64?.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase) != true)
            {
                return BadRequest("Choose an army type or provide a name and PNG icon.");
            }

            try
            {
                var imageBytes = Convert.FromBase64String(request.NewDefinitionImageBase64[("data:image/png;base64,".Length)..]);
                if (imageBytes.Length == 0 || imageBytes.Length > 256 * 1024)
                {
                    return BadRequest("The army icon is too large or empty.");
                }
            }
            catch (FormatException)
            {
                return BadRequest("The army icon is invalid.");
            }

            definition = new UnitDefinitionApiDto
            {
                Name = request.NewDefinitionName.Trim(),
                ImageBase64 = request.NewDefinitionImageBase64
            };
            state.UnitDefinitions.Add(definition);
        }

        state.Units.Add(new UnitApiDto
        {
            DefinitionId = definition.Id,
            FactionName = player!.FactionName,
            X = request.X,
            Y = request.Y,
            CurrentZoneName = zone.Name
        });
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpDelete("{id}/units/{unitId}")]
    public async Task<IActionResult> DeleteUnit(int id, string unitId)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        if (state!.Phase is not (TurnPhase.Moving or TurnPhase.PostCombatMoving) ||
            state.ConfirmedMovementFactions.Contains(player!.FactionName))
        {
            return BadRequest("Armies can be removed only before your faction confirms movement.");
        }

        var unit = state.Units.FirstOrDefault(candidate => candidate.Id == unitId);
        if (unit == null)
        {
            return NotFound("Army not found.");
        }

        if (!string.Equals(unit.FactionName, player.FactionName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        state.Units.Remove(unit);
        if (state.MovementDrafts.TryGetValue(player.FactionName, out var draft))
        {
            draft.RemoveAll(maneuver => maneuver.UnitId == unitId);
        }

        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/maneuver/draft")]
    public async Task<IActionResult> SaveMovementDraft(int id, [FromBody] List<UnitManeuver> maneuvers)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        if (state!.Phase is not (TurnPhase.Moving or TurnPhase.PostCombatMoving))
        {
            return BadRequest("Movement drafts are not available in this phase.");
        }

        if (state.ConfirmedMovementFactions.Contains(player!.FactionName))
        {
            return BadRequest("Your faction has already confirmed its movement.");
        }

        var validationError = ValidateManeuvers(state, player.FactionName, maneuvers);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        state.MovementDrafts[player.FactionName] = maneuvers;
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"Campaign_{id}_Faction_{player.FactionName}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/maneuver/confirm")]
    public async Task<IActionResult> ConfirmManeuvers(int id)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        if (state!.Phase is not (TurnPhase.Moving or TurnPhase.PostCombatMoving))
        {
            return BadRequest("It is not a movement phase.");
        }

        if (!state.MovementDrafts.TryGetValue(player!.FactionName, out var maneuvers))
        {
            return BadRequest("Save a movement draft before confirming.");
        }

        var validationError = ValidateManeuvers(state, player.FactionName, maneuvers);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        state.PendingManeuvers[player.FactionName] = maneuvers;
        if (!state.ConfirmedMovementFactions.Contains(player.FactionName))
        {
            state.ConfirmedMovementFactions.Add(player.FactionName);
        }

        state.MovementDrafts.Remove(player.FactionName);

        if (state.Factions.All(faction => state.ConfirmedMovementFactions.Contains(faction.Name)))
        {
            ResolveConfirmedMovement(state);
        }

        campaign.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("join")]
    [Authorize]
    public async Task<IActionResult> JoinCampaign([FromBody] JoinRequestApiDto joinRequestApiDto)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized();
        }

        var code = joinRequestApiDto.JoinCode.ToUpper().Trim();
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

        var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson, _jsonOptions);
        if (state == null)
        {
            return BadRequest("Invalid game state");
        }

        var targetFactionName = "";

        campaign.Players.Add(new CampaignPlayer
        {
            User = user,
            FactionName = joinRequestApiDto.FactionName,
            IsAlive = true,
            IsTurn = true
        });
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{campaign.Id}").SendAsync("GameUpdated");
        return Ok(new JoinResultApiDto { campaignId = campaign.Id, message = "Welcome to the campaign, Commander." });
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

        if (campaign == null)
        {
            return NotFound("Unknown Operation Code.");
        }

        var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson, _jsonOptions);
        var factionNames = state?.Factions.Select(f => f.Name).ToList();

        return Ok(new
        {
            campaign.Name,
            Factions = factionNames
        });
    }

    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized();
        }

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == id);
        if (campaign == null)
        {
            return NotFound("Campaign Not Found");
        }

        if (campaign.OwnerId != user.Id)
        {
            return Forbid("Only the campaign owner can delete the campaign.");
        }

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Campaign Deleted" });
    }

    [HttpPost("{id}/battle/configure")]
    public async Task<IActionResult> ConfigureBattle(int id, [FromBody] ConfigureBattleApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var battle = state!.ActiveBattles.FirstOrDefault(candidate => candidate.Id == request.BattleId && !candidate.IsResolved);
        if (state.Phase != TurnPhase.Combat || battle == null)
        {
            return BadRequest("This battle cannot be configured.");
        }

        if (!battle.Factions.Contains(player!.FactionName))
        {
            return Forbid();
        }

        if (request.IsAmphibious && (!battle.Factions.Contains(request.AttackerFaction) || !battle.Factions.Contains(request.DefenderFaction) || request.AttackerFaction == request.DefenderFaction))
        {
            return BadRequest("Select opposing attacker and defender factions.");
        }

        battle.IsAmphibious = request.IsAmphibious;
        battle.AttackerFaction = request.IsAmphibious ? request.AttackerFaction : string.Empty;
        battle.DefenderFaction = request.IsAmphibious ? request.DefenderFaction : string.Empty;
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/battle")]
    public async Task<IActionResult> BattleResult(int id, [FromBody] BattleResultApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var battle = state!.ActiveBattles.FirstOrDefault(candidate => candidate.Id == request.BattleId && !candidate.IsResolved);
        if (state.Phase != TurnPhase.Combat || battle == null)
        {
            return BadRequest("This battle is unavailable.");
        }

        if (!battle.Factions.Contains(player!.FactionName))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.ScenarioName))
        {
            return BadRequest("A scenario name is required.");
        }

        var stanceKey = GetBattleStanceKey(battle);
        if (!battle.IsAmphibious && !battle.Factions.All(faction => state.PendingStances.TryGetValue(stanceKey, out var stances) && stances.ContainsKey(faction)))
        {
            return BadRequest("Every faction must select a stance before resolving this battle.");
        }

        request.ZoneName = battle.ZoneName;
        request.TurnNumber = state.TurnNumber;
        request.IsAmphibious = battle.IsAmphibious;
        request.AttackerFaction = battle.AttackerFaction;
        request.DefenderFaction = battle.DefenderFaction;
        request.CommanderUsername = User.Identity?.Name ?? string.Empty;
        request.Stances = battle.IsAmphibious
            ? new Dictionary<string, BattleStance>()
            : state.PendingStances[stanceKey];
        request.WinnerFactions = CalculateWinnerFactions(request.MajorPoints, request.MinorPoints, battle.Factions);

        foreach (var unitFile in request.UpdatedUnitFiles)
        {
            var unit = state.Units.FirstOrDefault(candidate => candidate.Id == unitFile.Key);
            if (unit != null)
            {
                unit.ExcelDatabase64 = unitFile.Value;
            }
        }

        battle.IsResolved = true;
        battle.WinnerFactions = request.WinnerFactions;
        battle.ScenarioName = request.ScenarioName;
        battle.CommanderUsername = request.CommanderUsername;
        if (battle.IsSectorSwap)
        {
            battle.ColoringResolved = true;
            battle.ColoringSkipped = true;
        }
        state.BattleResults.Add(request);
        state.PendingStances.Remove(stanceKey);
        ContinueInterceptedUnits(state, battle);
        ReconcilePendingUncontestedColors(state);

        _context.BattleLogs.Add(new BattleLog
        {
            CampaignId = campaign!.Id,
            ZoneName = battle.ZoneName,
            TurnNumber = state.TurnNumber,
            ResultJson = JsonSerializer.Serialize(request, _jsonOptions)
        });

        if (state.ActiveBattles.All(candidate => candidate.IsResolved))
        {
            state.Phase = TurnPhase.Coloring;
            AdvanceAfterColoring(state);
        }

        campaign.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/battle/color")]
    public async Task<IActionResult> ColorBattle(int id, [FromBody] BattleColoringApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var battle = state!.ActiveBattles.FirstOrDefault(candidate => candidate.Id == request.BattleId && candidate.IsResolved && !candidate.ColoringResolved);
        if (state.Phase != TurnPhase.Coloring || battle == null || battle.IsSectorSwap)
        {
            return BadRequest("This battle is not awaiting coloring.");
        }

        if (!string.Equals(battle.CommanderUsername, User.Identity?.Name, StringComparison.Ordinal))
        {
            return Forbid();
        }

        if (!request.Skip && !state.Factions.Any(faction => faction.Name == request.FactionName))
        {
            return BadRequest("Unknown faction.");
        }

        battle.ColoringResolved = true;
        battle.ColoringSkipped = request.Skip;
        battle.ColoringFactionName = request.Skip ? string.Empty : request.FactionName;
        if (!request.Skip)
        {
            var zone = state.Zones.FirstOrDefault(candidate => candidate.Name == battle.ZoneName);
            if (zone != null)
            {
                zone.FactionName = request.FactionName;
            }
        }

        AdvanceAfterColoring(state);
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/sector/color")]
    public async Task<IActionResult> ColorUncontestedSector(int id, [FromBody] ZoneColoringApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        if (state!.Phase != TurnPhase.Coloring ||
            !state.PendingUncontestedColors.TryGetValue(request.ZoneName, out var occupyingFaction))
        {
            return BadRequest("This sector is not awaiting manual coloring.");
        }

        if (!string.Equals(occupyingFaction, player!.FactionName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (state.ActiveBattles.Any(battle => !battle.IsSectorSwap && string.Equals(battle.ZoneName, request.ZoneName, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Resolve the battle before coloring this sector.");
        }

        var zone = state.Zones.FirstOrDefault(candidate => string.Equals(candidate.Name, request.ZoneName, StringComparison.OrdinalIgnoreCase));
        if (zone == null)
        {
            return NotFound("Sector not found.");
        }

        zone.FactionName = player.FactionName;
        state.PendingUncontestedColors.Remove(request.ZoneName);
        AdvanceAfterColoring(state);
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    [HttpPost("{id}/stance")]
    public async Task<IActionResult> SubmitStance(int id, [FromBody] SubmitStanceApiDto request)
    {
        var (campaign, player, state, error) = await LoadCampaignStateAsync(id);
        if (error != null)
        {
            return error;
        }

        var battle = state!.ActiveBattles.FirstOrDefault(candidate =>
            !candidate.IsResolved &&
            (string.IsNullOrWhiteSpace(request.BattleId)
                ? string.Equals(candidate.ZoneName, request.ZoneName, StringComparison.OrdinalIgnoreCase)
                : candidate.Id == request.BattleId));
        if (state.Phase != TurnPhase.Combat || battle == null || battle.IsAmphibious)
        {
            return BadRequest("Stances are unavailable for this battle.");
        }

        if (!battle.Factions.Contains(player!.FactionName))
        {
            return Forbid();
        }

        var stanceKey = GetBattleStanceKey(battle);
        if (!state.PendingStances.ContainsKey(stanceKey))
        {
            state.PendingStances[stanceKey] = new();
        }

        state.PendingStances[stanceKey][player.FactionName] = request.Stance;
        campaign!.GameStateJson = JsonSerializer.Serialize(state, _jsonOptions);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"Campaign_{id}").SendAsync("GameUpdated");
        return Ok();
    }

    private static List<string> CalculateWinnerFactions(Dictionary<string, int> majorPoints, Dictionary<string, int> minorPoints, IEnumerable<string> factions)
    {
        var scores = factions.Select(faction => new
        {
            Faction = faction,
            Major = majorPoints.TryGetValue(faction, out var major) ? major : 0,
            Minor = minorPoints.TryGetValue(faction, out var minor) ? minor : 0
        }).ToList();
        var highestMajor = scores.Max(score => score.Major);
        var majorWinners = scores.Where(score => score.Major == highestMajor).ToList();
        var highestMinor = majorWinners.Max(score => score.Minor);
        return majorWinners.Where(score => score.Minor == highestMinor).Select(score => score.Faction).ToList();
    }

    private void ContinueInterceptedUnits(GameStateDto state, ActiveBattleApiDto battle)
    {
        if (!battle.InterceptedUnitIds.Any())
        {
            return;
        }

        var plans = BuildProjectedManeuvers(state, state.PendingManeuvers.Values.SelectMany(maneuvers => maneuvers)).ToDictionary(maneuver => maneuver.UnitId);
        var units = state.Units.ToDictionary(unit => unit.Id);
        var battleZone = state.Zones.FirstOrDefault(zone => zone.Name == battle.ZoneName);

        foreach (var unitId in battle.InterceptedUnitIds)
        {
            if (!plans.TryGetValue(unitId, out var plan) || !units.TryGetValue(unitId, out var unit))
            {
                continue;
            }

            if (battle.WinnerFactions.Contains(unit.FactionName))
            {
                var continuingPlans = plans.Values.Where(candidate =>
                    !battle.InterceptedUnitIds.Contains(candidate.UnitId) ||
                    (units.TryGetValue(candidate.UnitId, out var candidateUnit) && battle.WinnerFactions.Contains(candidateUnit.FactionName)));
                CreateDeferredDestinationBattle(state, plan.DestinationZoneName, continuingPlans);
                continue;
            }

            if (battleZone != null)
            {
                unit.X = battleZone.X;
                unit.Y = battleZone.Y;
                unit.CurrentZoneName = battle.ZoneName;
            }
        }
    }

    private void CreateDeferredDestinationBattle(GameStateDto state, string destinationZoneName, IEnumerable<UnitManeuver> plans)
    {
        if (state.ActiveBattles.Any(battle => !battle.IsSectorSwap && string.Equals(battle.ZoneName, destinationZoneName, StringComparison.OrdinalIgnoreCase) && !battle.IsResolved))
        {
            return;
        }

        var unitsById = state.Units.ToDictionary(unit => unit.Id);
        var participants = plans.Where(plan => string.Equals(plan.DestinationZoneName, destinationZoneName, StringComparison.OrdinalIgnoreCase))
            .Select(plan => plan.UnitId).Where(unitsById.ContainsKey).Distinct().ToList();
        var factions = participants.Select(unitId => unitsById[unitId].FactionName).Distinct().ToList();
        if (factions.Count >= 2)
        {
            state.PendingUncontestedColors.Remove(destinationZoneName);
            state.ActiveBattles.Add(CreateBattle(destinationZoneName, state.TurnNumber, factions, participants, []));
            return;
        }

        if (factions.Count == 1)
        {
            QueueUncontestedColor(state, destinationZoneName, factions[0]);
        }
    }

    private async Task<(Campaign? Campaign, CampaignPlayer? Player, GameStateDto? State, IActionResult? Error)> LoadCampaignStateAsync(int id)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Username == username);
        if (user == null)
        {
            return (null, null, null, Unauthorized());
        }

        var campaign = await _context.Campaigns.Include(candidate => candidate.Players)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (campaign == null)
        {
            return (null, null, null, NotFound("Campaign Not Found"));
        }

        var player = campaign.Players.FirstOrDefault(member => member.UserId == user.Id);
        if (player == null)
        {
            return (null, null, null, Unauthorized("You are not a member of this campaign"));
        }

        var state = JsonSerializer.Deserialize<GameStateDto>(campaign.GameStateJson, _jsonOptions);
        return state == null
            ? (null, null, null, BadRequest("Invalid game state"))
            : (campaign, player, state, null);
    }

    private string? ValidateManeuvers(GameStateDto state, string factionName, List<UnitManeuver> maneuvers)
    {
        if (state.Zones.GroupBy(zone => zone.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            return "Every sector must have a unique name before movement can begin.";
        }

        var zoneNames = state.Zones.Select(zone => zone.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (state.Units.Any(unit => string.IsNullOrWhiteSpace(unit.CurrentZoneName) || !zoneNames.Contains(unit.CurrentZoneName)))
        {
            return "Unit positions are still being synchronized with the map. Wait a moment and try again.";
        }

        var ownUnitIds = state.Units.Where(unit => unit.FactionName == factionName).Select(unit => unit.Id).OrderBy(id => id).ToList();
        var submittedUnitIds = maneuvers.Select(maneuver => maneuver.UnitId).OrderBy(id => id).ToList();
        if (!ownUnitIds.SequenceEqual(submittedUnitIds))
        {
            return "A movement draft must include every unit of your faction exactly once.";
        }

        var zonesByName = state.Zones.ToDictionary(zone => zone.Name, StringComparer.OrdinalIgnoreCase);
        var unitsById = state.Units.ToDictionary(unit => unit.Id);
        foreach (var maneuver in maneuvers)
        {
            if (!zonesByName.TryGetValue(maneuver.OriginZoneName, out var origin) ||
                !zonesByName.TryGetValue(maneuver.DestinationZoneName, out var destination))
            {
                return "A movement references an unknown sector.";
            }

            if (maneuver.DigIn &&
                (!unitsById.TryGetValue(maneuver.UnitId, out var unit) ||
                 !string.Equals(origin.Name, unit.CurrentZoneName, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(origin.Name, destination.Name, StringComparison.OrdinalIgnoreCase) ||
                 !string.IsNullOrWhiteSpace(maneuver.IntermediateZoneName) ||
                 maneuver.IsMovementOrder))
            {
                return "A unit can dig in only instead of moving from its current sector.";
            }

            /* Movement range and ownership restrictions are temporarily disabled.
               Keep this validation here for when route rules are refined.
            if (string.Equals(origin.Name, destination.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(maneuver.IntermediateZoneName))
                {
                    return "A stationary unit cannot have an intermediate sector.";
                }

                continue;
            }

            if (!state.AdjacencyGraph.TryGetValue(origin.Name, out var originNeighbors))
            {
                return "The map has no adjacency data for this sector.";
            }

            if (string.IsNullOrWhiteSpace(maneuver.IntermediateZoneName))
            {
                if (!originNeighbors.Contains(destination.Name, StringComparer.OrdinalIgnoreCase))
                {
                    return "A unit can move only to an adjacent sector.";
                }

                continue;
            }

            if (!zonesByName.TryGetValue(maneuver.IntermediateZoneName, out var intermediate) ||
                !originNeighbors.Contains(intermediate.Name, StringComparer.OrdinalIgnoreCase) ||
                !state.AdjacencyGraph.TryGetValue(intermediate.Name, out var intermediateNeighbors) ||
                !intermediateNeighbors.Contains(destination.Name, StringComparer.OrdinalIgnoreCase))
            {
                return "The selected two-sector route is not adjacent.";
            }

            if (!string.Equals(intermediate.FactionName, factionName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(destination.FactionName, factionName, StringComparison.OrdinalIgnoreCase))
            {
                return "Both sectors of a two-sector move must belong to your faction.";
            }
            */
        }

        return null;
    }

    private void ResolveConfirmedMovement(GameStateDto state)
    {
        var wasPostCombatMovement = state.Phase == TurnPhase.PostCombatMoving;
        var plans = BuildProjectedManeuvers(state, state.PendingManeuvers.Values.SelectMany(maneuvers => maneuvers));
        var unitById = state.Units.ToDictionary(unit => unit.Id);

        foreach (var plan in plans)
        {
            if (!unitById.TryGetValue(plan.UnitId, out var unit))
            {
                continue;
            }

            if (plan.DigIn)
            {
                unit.IsDugIn = true;
                continue;
            }

            if (plan.IsMovementOrder ||
                !string.Equals(plan.OriginZoneName, plan.DestinationZoneName, StringComparison.OrdinalIgnoreCase))
            {
                unit.IsDugIn = false;
            }

            unit.X = plan.TargetX;
            unit.Y = plan.TargetY;
            unit.CurrentZoneName = plan.DestinationZoneName;
        }

        state.ActiveBattles = CreateInitialBattles(state, plans);
        state.PendingUncontestedColors.Clear();
        QueueUncontestedColors(state, plans);
        ReconcilePendingUncontestedColors(state);
        state.MovementDrafts.Clear();
        state.ConfirmedMovementFactions.Clear();
        state.ColoringCompletesRound = wasPostCombatMovement;

        if (state.ActiveBattles.Any())
        {
            state.Phase = TurnPhase.Combat;
            return;
        }

        state.PendingManeuvers.Clear();
        if (state.PendingUncontestedColors.Count > 0)
        {
            state.Phase = TurnPhase.Coloring;
            return;
        }
        if (wasPostCombatMovement)
        {
            StartNextRound(state);
        }
        else
        {
            state.Phase = TurnPhase.PostCombatMoving;
        }
    }

    private List<ActiveBattleApiDto> CreateInitialBattles(GameStateDto state, List<UnitManeuver> plans)
    {
        var result = new List<ActiveBattleApiDto>();
        var unitById = state.Units.ToDictionary(unit => unit.Id);
        var plansByDestination = plans.GroupBy(plan => plan.DestinationZoneName, StringComparer.OrdinalIgnoreCase).ToList();
        var interceptedUnitIds = new HashSet<string>();

        foreach (var destinationGroup in plansByDestination)
        {
            var hasArrivingUnit = destinationGroup.Any(plan =>
                !string.Equals(plan.OriginZoneName, plan.DestinationZoneName, StringComparison.OrdinalIgnoreCase));
            if (!hasArrivingUnit)
            {
                continue;
            }

            var passingPlans = plans.Where(plan => string.Equals(plan.IntermediateZoneName, destinationGroup.Key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!passingPlans.Any())
            {
                continue;
            }

            var participantIds = destinationGroup.Select(plan => plan.UnitId).Concat(passingPlans.Select(plan => plan.UnitId)).Distinct().ToList();
            var factions = participantIds.Where(unitById.ContainsKey).Select(id => unitById[id].FactionName).Distinct().ToList();
            if (factions.Count < 2)
            {
                continue;
            }

            result.Add(CreateBattle(destinationGroup.Key, state.TurnNumber, factions, participantIds, passingPlans.Select(plan => plan.UnitId)));
            foreach (var passingPlan in passingPlans)
            {
                interceptedUnitIds.Add(passingPlan.UnitId);
            }
        }

        foreach (var destinationGroup in plansByDestination)
        {
            var hasArrivingUnit = destinationGroup.Any(plan =>
                !string.Equals(plan.OriginZoneName, plan.DestinationZoneName, StringComparison.OrdinalIgnoreCase));
            if (!hasArrivingUnit)
            {
                continue;
            }

            var participantIds = destinationGroup.Select(plan => plan.UnitId).Where(id => !interceptedUnitIds.Contains(id)).Distinct().ToList();
            var factions = participantIds.Where(unitById.ContainsKey).Select(id => unitById[id].FactionName).Distinct().ToList();
            if (factions.Count < 2 || result.Any(battle => string.Equals(battle.ZoneName, destinationGroup.Key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(CreateBattle(destinationGroup.Key, state.TurnNumber, factions, participantIds, []));
        }

        result.AddRange(CreateSectorSwapBattles(state, plans, unitById));
        return result;
    }

    private static List<ActiveBattleApiDto> CreateSectorSwapBattles(
        GameStateDto state, List<UnitManeuver> plans, Dictionary<string, UnitApiDto> unitById)
    {
        var battles = new List<ActiveBattleApiDto>();
        var zones = state.Zones.OrderBy(zone => zone.Name, StringComparer.OrdinalIgnoreCase).ToList();

        for (var first = 0; first < zones.Count; first++)
        {
            for (var second = first + 1; second < zones.Count; second++)
            {
                var zoneA = zones[first].Name;
                var zoneB = zones[second].Name;
                if (!AreAdjacent(state, zoneA, zoneB))
                {
                    continue;
                }

                var fromAToB = plans.Where(plan =>
                    string.Equals(plan.OriginZoneName, zoneA, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(plan.DestinationZoneName, zoneB, StringComparison.OrdinalIgnoreCase)).ToList();
                var fromBToA = plans.Where(plan =>
                    string.Equals(plan.OriginZoneName, zoneB, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(plan.DestinationZoneName, zoneA, StringComparison.OrdinalIgnoreCase)).ToList();
                if (fromAToB.Count == 0 || fromBToA.Count == 0)
                {
                    continue;
                }

                var crossingIds = fromAToB.Select(plan => plan.UnitId)
                    .Concat(fromBToA.Select(plan => plan.UnitId))
                    .Distinct()
                    .ToList();
                var factions = crossingIds.Where(unitById.ContainsKey)
                    .Select(id => unitById[id].FactionName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (factions.Count < 2)
                {
                    continue;
                }

                var battle = CreateBattle($"{zoneA} - {zoneB}", state.TurnNumber, factions, crossingIds, []);
                battle.IsSectorSwap = true;
                battle.SwapZoneA = zoneA;
                battle.SwapZoneB = zoneB;
                battles.Add(battle);
            }
        }

        return battles;
    }

    private static bool AreAdjacent(GameStateDto state, string zoneA, string zoneB) =>
        state.AdjacencyGraph.Any(entry =>
            (string.Equals(entry.Key, zoneA, StringComparison.OrdinalIgnoreCase) &&
             entry.Value.Contains(zoneB, StringComparer.OrdinalIgnoreCase)) ||
            (string.Equals(entry.Key, zoneB, StringComparison.OrdinalIgnoreCase) &&
             entry.Value.Contains(zoneA, StringComparer.OrdinalIgnoreCase)));

    private static List<UnitManeuver> BuildProjectedManeuvers(GameStateDto state, IEnumerable<UnitManeuver> submittedManeuvers)
    {
        var maneuversByUnitId = submittedManeuvers.ToDictionary(maneuver => maneuver.UnitId);
        foreach (var unit in state.Units)
        {
            if (maneuversByUnitId.ContainsKey(unit.Id))
            {
                continue;
            }

            maneuversByUnitId[unit.Id] = new UnitManeuver
            {
                UnitId = unit.Id,
                TargetX = unit.X,
                TargetY = unit.Y,
                OriginZoneName = unit.CurrentZoneName,
                DestinationZoneName = unit.CurrentZoneName
            };
        }

        return maneuversByUnitId.Values.ToList();
    }

    private static ActiveBattleApiDto CreateBattle(string zoneName, int turnNumber, IEnumerable<string> factions, IEnumerable<string> unitIds, IEnumerable<string> interceptedUnitIds) => new()
    {
        ZoneName = zoneName,
        TurnNumber = turnNumber,
        Factions = factions.Distinct().ToList(),
        UnitIds = unitIds.Distinct().ToList(),
        InterceptedUnitIds = interceptedUnitIds.Distinct().ToList()
    };

    private static string GetBattleStanceKey(ActiveBattleApiDto battle) =>
        battle.IsSectorSwap ? battle.Id : battle.ZoneName;

    private static void QueueUncontestedColors(GameStateDto state, IEnumerable<UnitManeuver> plans)
    {
        var unitsById = state.Units.ToDictionary(unit => unit.Id);
        var swapZones = state.ActiveBattles.Where(battle => battle.IsSectorSwap)
            .SelectMany(battle => new[] { battle.SwapZoneA, battle.SwapZoneB })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var group in plans.GroupBy(plan => plan.DestinationZoneName, StringComparer.OrdinalIgnoreCase))
        {
            if (swapZones.Contains(group.Key))
            {
                continue;
            }

            if (!group.Any(plan => !string.Equals(plan.OriginZoneName, plan.DestinationZoneName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (state.ActiveBattles.Any(battle => !battle.IsSectorSwap && string.Equals(battle.ZoneName, group.Key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var factions = group.Where(plan => unitsById.ContainsKey(plan.UnitId))
                .Select(plan => unitsById[plan.UnitId].FactionName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (factions.Count == 1)
            {
                QueueUncontestedColor(state, group.Key, factions[0]);
            }
        }
    }

    private static void QueueUncontestedColor(GameStateDto state, string zoneName, string factionName)
    {
        var zone = state.Zones.FirstOrDefault(candidate => string.Equals(candidate.Name, zoneName, StringComparison.OrdinalIgnoreCase));
        if (zone != null && !string.Equals(zone.FactionName, factionName, StringComparison.OrdinalIgnoreCase))
        {
            state.PendingUncontestedColors[zone.Name] = factionName;
        }
    }

    private static void ReconcilePendingUncontestedColors(GameStateDto state)
    {
        var swapZones = state.ActiveBattles.Where(battle => battle.IsSectorSwap)
            .SelectMany(battle => new[] { battle.SwapZoneA, battle.SwapZoneB })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pending in state.PendingUncontestedColors.ToList())
        {
            var zone = state.Zones.FirstOrDefault(candidate => string.Equals(candidate.Name, pending.Key, StringComparison.OrdinalIgnoreCase));
            var occupyingFactions = state.Units
                .Where(unit => string.Equals(unit.CurrentZoneName, pending.Key, StringComparison.OrdinalIgnoreCase))
                .Select(unit => unit.FactionName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (swapZones.Contains(pending.Key) || zone == null || occupyingFactions.Count != 1 ||
                !string.Equals(occupyingFactions[0], pending.Value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zone.FactionName, pending.Value, StringComparison.OrdinalIgnoreCase) ||
                state.ActiveBattles.Any(battle => !battle.IsSectorSwap && string.Equals(battle.ZoneName, pending.Key, StringComparison.OrdinalIgnoreCase)))
            {
                state.PendingUncontestedColors.Remove(pending.Key);
            }
        }
    }

    private static void AdvanceAfterColoring(GameStateDto state)
    {
        if (state.PendingUncontestedColors.Count > 0 || state.ActiveBattles.Any(battle => !battle.ColoringResolved))
        {
            return;
        }

        state.PendingManeuvers.Clear();
        if (state.ColoringCompletesRound)
        {
            StartNextRound(state);
        }
        else
        {
            state.Phase = TurnPhase.PostCombatMoving;
        }

        state.ColoringCompletesRound = false;
    }

    private static void StartNextRound(GameStateDto state)
    {
        state.TurnNumber++;
        state.Phase = TurnPhase.Moving;
        state.ColoringCompletesRound = false;
        state.ActiveBattles.Clear();
        state.PendingStances.Clear();
    }

    private string SerializeStateForFaction(GameStateDto state, string factionName)
    {
        var visibleState = JsonSerializer.Deserialize<GameStateDto>(JsonSerializer.Serialize(state, _jsonOptions), _jsonOptions)!;
        visibleState.MovementDrafts = visibleState.MovementDrafts
            .Where(entry => string.Equals(entry.Key, factionName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        visibleState.PendingManeuvers = visibleState.PendingManeuvers
            .Where(entry => string.Equals(entry.Key, factionName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        return JsonSerializer.Serialize(visibleState, _jsonOptions);
    }
}
