using FowCampaign.Api.Modules.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FowCampaign.Api.Modules.SignalR;

[Authorize]
public class GameHub(FowCampaignContext context) : Hub
{
    public async Task JoinCampaignGroup(int campaignId)
    {
        var username = Context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            throw new HubException("Authentication is required.");

        var factionName = await context.Campaigns
            .Where(campaign => campaign.Id == campaignId)
            .SelectMany(campaign => campaign.Players)
            .Where(player => player.User.Username == username)
            .Select(player => player.FactionName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(factionName))
            throw new HubException("You are not a member of this campaign.");

        await Groups.AddToGroupAsync(Context.ConnectionId, CampaignGroupName(campaignId));
        await Groups.AddToGroupAsync(Context.ConnectionId, FactionGroupName(campaignId, factionName));
    }

    public async Task LeaveCampaignGroup(int campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, CampaignGroupName(campaignId));
    }

    private static string CampaignGroupName(int campaignId) => $"Campaign_{campaignId}";
    private static string FactionGroupName(int campaignId, string factionName) => $"Campaign_{campaignId}_Faction_{factionName}";
}
