﻿using System;
using Dalamud.ContextMenu;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using PlayerTrack.Domain;

namespace PlayerTrack.Plugin;

using Dalamud.Game.Text;
using Domain.Common;

public static class ContextMenuHandler
{
    private static GameObjectContextMenuItem? openPlayerTrackMenuItem;
    private static GameObjectContextMenuItem? openLodestoneItem;
    private static DalamudContextMenu? contextMenu;
    private static bool isStarted;

    public delegate void SelectPlayerDelegate(ToadPlayer player, bool isCurrent);

    public static event SelectPlayerDelegate? SelectPlayer;

    public static void Start()
    {
        DalamudContext.PluginLog.Verbose("Entering ContextMenuHandler.Start()");
        contextMenu = new DalamudContextMenu(DalamudContext.PluginInterface);
        contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
        BuildContentMenuItems();
        isStarted = true;
    }

    public static void Restart()
    {
        DalamudContext.PluginLog.Verbose("Entering ContextMenuHandler.Restart()");
        Dispose();
        Start();
    }

    public static void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering ContextMenuHandler.Dispose()");
        try
        {
            if (contextMenu == null) return;
            contextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
            contextMenu.Dispose();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to dispose ContextMenuHandler properly.");
        }
    }

    private static void BuildContentMenuItems()
    {
        DalamudContext.PluginLog.Verbose("Entering ContextMenuHandler.BuildContentMenuItems()");
        var showContextMenuIndicator = ServiceContext.ConfigService.GetConfig().ShowContextMenuIndicator;
        SeString openPlayerTrackMenuItemName;
        SeString openLodestoneItemName;
        if (showContextMenuIndicator)
        {
            openPlayerTrackMenuItemName = new SeString().Append(new UIForegroundPayload(539)).Append($"{SeIconChar.BoxedLetterP.ToIconString()} ")
                .Append(new UIForegroundPayload(0)).Append(DalamudContext.LocManager.GetString("OpenPlayerTrack"));
            openLodestoneItemName = new SeString().Append(new UIForegroundPayload(539)).Append($"{SeIconChar.BoxedLetterP.ToIconString()} ")
                .Append(new UIForegroundPayload(0)).Append(DalamudContext.LocManager.GetString("OpenLodestone"));
        }
        else
        {
            openPlayerTrackMenuItemName = new SeString(new TextPayload(DalamudContext.LocManager.GetString("OpenPlayerTrack")));
            openLodestoneItemName = new SeString(new TextPayload(DalamudContext.LocManager.GetString("OpenLodestone")));
        }

        openPlayerTrackMenuItem = new GameObjectContextMenuItem(openPlayerTrackMenuItemName, OnSelectPlayer);
        openLodestoneItem = new GameObjectContextMenuItem(openLodestoneItemName, OnOpenLodestone);
    }

    private static void OnOpenLodestone(BaseContextMenuArgs args)
    {
        DalamudContext.PluginLog.Verbose($"Entering ContextMenuHandler.OnOpenLodestone(): {args.Text} {args.ObjectWorld}");
        if (args.Text == null || !args.Text.IsValidCharacterName())
        {
            DalamudContext.PluginLog.Warning($"Invalid character name: {args.Text}");
            return;
        }

        var key = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
        var player = ServiceContext.PlayerDataService.GetPlayer(key);
        var lodestoneId = player?.LodestoneId ?? 0;
        PlayerLodestoneService.OpenLodestoneProfile(lodestoneId);
    }

    private static void OnSelectPlayer(GameObjectContextMenuItemSelectedArgs args)
    {
        DalamudContext.PluginLog.Verbose($"Entering ContextMenuHandler.OnSelectPlayer(), {args.Text} {args.ObjectWorld}");
        if (args.Text == null || !args.Text.IsValidCharacterName())
        {
            DalamudContext.PluginLog.Warning($"Invalid character name: {args.Text}");
            return;
        }

        bool isCurrent;
        var toadPlayer = DalamudContext.PlayerEventDispatcher.GetPlayerByNameAndWorldId(args.Text.TextValue, args.ObjectWorld);
        if (toadPlayer == null)
        {
            toadPlayer = new ToadPlayer
            {
                Name = args.Text.TextValue,
                HomeWorld = args.ObjectWorld,
                CompanyTag = string.Empty,
            };
            isCurrent = false;
        }
        else
        {
            isCurrent = true;
        }

        SelectPlayer?.Invoke(toadPlayer, isCurrent);
    }

    private static bool IsMenuValid(BaseContextMenuArgs args)
    {
        DalamudContext.PluginLog.Verbose($"Entering ContextMenuHandler.IsMenuValid(), ParentAddonName: {args.ParentAddonName}");
        switch (args.ParentAddonName)
        {
            case null:
            case "LookingForGroup":
            case "PartyMemberList":
            case "FriendList":
            case "FreeCompany":
            case "SocialList":
            case "ContactList":
            case "ChatLog":
            case "_PartyList":
            case "LinkShell":
            case "CrossWorldLinkshell":
            case "ContentMemberList":
            case "BlackList":
            case "BeginnerChatList":
                return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

            default:
                DalamudContext.PluginLog.Verbose($"Invalid ParentAddonName: {args.ParentAddonName}");
                return false;
        }
    }

    private static void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        DalamudContext.PluginLog.Verbose($"Entering ContextMenuHandler.OpenGameObjectContextMenu(), ObjectId: {args.ObjectId}, ParentAddonName: {args.ParentAddonName}");

        // check if plugin started
        if (!isStarted)
        {
            return;
        }

        // validate menu
        if (!IsMenuValid(args))
        {
            return;
        }
        
        // name can't be null
        if (string.IsNullOrEmpty(args.Text?.TextValue))
        {
            return;
        }

        // get self key
        var name = DalamudContext.ClientStateHandler.LocalPlayer?.Name.TextValue;
        var worldId = DalamudContext.ClientStateHandler.LocalPlayer?.HomeWorld.Id;
        if (string.IsNullOrEmpty(name) || worldId == null)
        {
            DalamudContext.PluginLog.Warning("Failed to get self key.");
            return;
        }

        // check if self menu
        var selfKey = PlayerKeyBuilder.Build(name, worldId.Value);
        var menuKey = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
        if (selfKey.Equals(menuKey, StringComparison.Ordinal))
        {
            DalamudContext.PluginLog.Verbose("Self menu, skipping.");
            return;
        }

        // add menu items
        if (ServiceContext.ConfigService.GetConfig().ShowOpenInPlayerTrack)
        {
            args.AddCustomItem(openPlayerTrackMenuItem);
        }

        if (!ServiceContext.ConfigService.GetConfig().ShowOpenLodestone) return;
        var key = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
        var player = ServiceContext.PlayerDataService.GetPlayer(key);
        var lodestoneId = player?.LodestoneId ?? 0;
        if (lodestoneId != 0)
        {
            args.AddCustomItem(openLodestoneItem);
        }
    }
}
