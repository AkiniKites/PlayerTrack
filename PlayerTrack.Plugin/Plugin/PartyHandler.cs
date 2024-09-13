using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using PlayerTrack.Domain.Common;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Game.Text.SeStringHandling;
using System.Xml.Linq;

namespace PlayerTrack.Plugin;

internal class PartyMemeber
{
    public uint Id{ get; set; }
    public string Name { get; set; }
    public uint HomeWorld{ get; set; }
}

internal delegate void PartyChangedDelegate(PartyMemeber member);

internal static class PartyHandler
{
    private const int TickDelay = 30;
    private static int ticks = TickDelay;

    private static Dictionary<uint, PartyMemeber> partyMemebers = new Dictionary<uint, PartyMemeber>();

    public static event PartyChangedDelegate PlayerJoined;
    public static event PartyChangedDelegate PlayerLeft;

    public static void Initialize()
    {
        DalamudContext.GameFramework.Update += GameFramework_Update;
        PlayerJoined += PartyHandler_PlayerJoined;
    }

    private static void PartyHandler_PlayerJoined(PartyMemeber member)
    {
        var player = ServiceContext.PlayerDataService.GetPlayer(member.Name, member.HomeWorld);

        if (player?.AssignedCategories.Any() == true)
        {
            var categories = player.AssignedCategories.Select(x => x.Name);
            DalamudContext.ChatGuiHandler.PluginPrintNotice($"{member.Name}: {String.Join(", ", categories)}");
        }
    }

    private static unsafe void GameFramework_Update(IFramework framework)
    {
        if (ticks++ < TickDelay)
            return;
        ticks = 0;

        var added = new HashSet<uint>();
        var self = DalamudContext.ClientStateHandler.LocalPlayer?.EntityId;

        foreach (var player in DalamudContext.PartyCollection)
        {
            if (player.ObjectId == self)
                continue;

            added.Add(player.ObjectId);
            MaybeAddPlayer(player.ObjectId, player.Name.ToString(), player.World.Id);
        }

        var cwProxy = InfoProxyCrossRealm.Instance();
        if (cwProxy->IsInCrossRealmParty != 0)
        {
            for (var i = 0; i < cwProxy->CrossRealmGroups.Length; i++)
            {
                var crossRealmGroup = cwProxy->CrossRealmGroups[i];
                for (var j = 0; j < crossRealmGroup.GroupMemberCount; j++)
                {
                    var player = crossRealmGroup.GroupMembers[j];

                    if (player.EntityId == self)
                        continue;

                    added.Add(player.EntityId);
                    var name = player.NameString;

                    MaybeAddPlayer(player.EntityId, name, (uint)player.HomeWorld);
                }
            }
        }

        foreach (var id in partyMemebers.Keys.ToList())
        {
            if (!added.Contains(id))
                MaybeRemovePlayer(id);
        }
    }

    private static void MaybeAddPlayer(uint id, string name, uint homeWorld)
    {
        if (partyMemebers.ContainsKey(id))
            return;

        var member = new PartyMemeber()
        {
            Id = id,
            Name = name,
            HomeWorld = homeWorld
        };

        partyMemebers.Add(id, member);
        PlayerJoined?.Invoke(member);
    }

    private static void MaybeRemovePlayer(uint id)
    {
        if (partyMemebers.Remove(id, out var member))
            PlayerLeft?.Invoke(member);
    }

    public static void Dispose()
    {
        DalamudContext.GameFramework.Update -= GameFramework_Update;
        PlayerJoined -= PartyHandler_PlayerJoined;
    }
}
