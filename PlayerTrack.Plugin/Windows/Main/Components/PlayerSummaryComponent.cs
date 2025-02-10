﻿using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;
using PlayerTrack.Windows.Main.Presenters;
using PlayerTrack.Windows.ViewModels;

namespace PlayerTrack.Windows.Main.Components;

public class PlayerSummaryComponent : ViewComponent
{
    private const float SectionSpace = 2.8f;

    private readonly IMainPresenter Presenter;
    private float[] CurrentOffsets = [];
    private int SelectedTagIndex;
    private int SelectedCategoryIndex;
    private float AssignedChildHeight;
    private bool IsLanguageChanged = true;

    public PlayerSummaryComponent(IMainPresenter presenter)
    {
        Presenter = presenter;
        Plugin.PluginInterface.LanguageChanged += _ => IsLanguageChanged = true;
    }

    public void CalcSize()
    {
        var offsets = new float[3];
        const string maxLengthName = "WWWWW";

        var maxNameWidth = Math.Max(ImGui.CalcTextSize(Language.Name).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxHomeWorldWidth = Math.Max(ImGui.CalcTextSize(Language.Homeworld).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxFreeCompanyWidth = Math.Max(ImGui.CalcTextSize(Language.FreeCompany).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxLodestoneWidth = Math.Max(ImGui.CalcTextSize(Language.Lodestone).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxAppearanceWidth = Math.Max(ImGui.CalcTextSize(Language.Appearance).X, ImGui.CalcTextSize(maxLengthName).X);

        var maxLastSeenWidth = Math.Max(ImGui.CalcTextSize(Language.LastSeen).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxSeenCountWidth = Math.Max(ImGui.CalcTextSize(Language.SeenCount).X, ImGui.CalcTextSize(maxLengthName).X);
        var maxLastLocationWidth = Math.Max(ImGui.CalcTextSize(Language.LastLocation).X, ImGui.CalcTextSize(maxLengthName).X);

        var maxFirstSeenWidth = Math.Max(ImGui.CalcTextSize(Language.FirstSeen).X, ImGui.CalcTextSize(maxLengthName).X);

        var maxOffset0Width = Math.Max(maxNameWidth, Math.Max(maxHomeWorldWidth, Math.Max(maxFreeCompanyWidth, Math.Max(maxLodestoneWidth, maxAppearanceWidth))));
        var maxOffset1Width = Math.Max(maxLastSeenWidth, Math.Max(maxSeenCountWidth, maxLastLocationWidth));
        var maxOffset2Width = maxFirstSeenWidth;

        offsets[0] = maxOffset0Width + (30f * ImGuiHelpers.GlobalScale);
        offsets[1] = offsets[0] + maxOffset1Width + (60f * ImGuiHelpers.GlobalScale);
        offsets[2] = offsets[1] + maxOffset2Width + (60f * ImGuiHelpers.GlobalScale);

        CurrentOffsets = offsets;
    }

    public override void Draw()
    {
        var player = Presenter.GetSelectedPlayer();
        if (player == null)
            return;

        if (IsLanguageChanged)
        {
            CalcSize();
            IsLanguageChanged = false;
        }

        using var child = ImRaii.Child("###PlayerSummaryPlayer", new Vector2(-1, 0), false);
        if (!child.Success)
            return;

        DrawInfoStatHeadings();
        DrawName(player);
        DrawFirstSeen(player);
        DrawHomeworld(player);
        DrawLastSeen(player);
        DrawFreeCompany(player);
        DrawLastLocation(player);
        DrawAppearance(player);
        DrawSeenCount(player);
        DrawCategoryTagHeadings();
        DrawCategoryTagAssignment(player);
        DrawCategoryTagAssignments(player);
        DrawNotes(player);
    }

    private void DrawNotes(PlayerView player)
    {
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Notes);
        var flags = ImGuiInputTextFlags.AllowTabInput;
        if (Config.UseCtrlNewLine)
            flags |= ImGuiInputTextFlags.CtrlEnterForNewLine;

        var notes = player.Notes;
        var inputSize = new Vector2(ImGui.GetWindowSize().X - (5f * ImGuiHelpers.GlobalScale), -1 - (5f * ImGuiHelpers.GlobalScale));
        if (ImGui.InputTextMultiline("###Player_Summary_Notes_Text", ref notes, 2000, inputSize, flags))
        {
            player.Notes = notes;
            ServiceContext.PlayerDataService.UpdatePlayerNotes(player.Id, notes);
        }
    }

    private void DrawCategoryTagAssignments(PlayerView player)
    {
        DrawAssignedCategories(player);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        DrawAssignedTags(player);
    }

    private void DrawCategoryTagAssignment(PlayerView player)
    {
        CalculateAssignedChildHeight(player);
        DrawCategoryCombo(player);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        DrawTagCombo(player);
        ImGuiHelpers.ScaledDummy(1f);
    }

    private void DrawCategoryTagHeadings()
    {
        ImGuiHelpers.ScaledDummy(SectionSpace);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Categories);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Tags);
    }

    private void DrawAppearance(PlayerView player)
    {
        ImGui.TextUnformatted(Language.Appearance);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[0]);
        ImGui.TextUnformatted(player.Appearance);
    }

    private void DrawSeenCount(PlayerView player)
    {
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        ImGui.TextUnformatted(Language.SeenCount);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[2]);
        ImGui.TextUnformatted(player.SeenCount);
    }

    private void DrawLastLocation(PlayerView player)
    {
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        ImGui.TextUnformatted(Language.LastLocation);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[2]);
        ImGui.TextUnformatted(player.LastLocation);
    }

    private void DrawFreeCompany(PlayerView player)
    {
        ImGui.TextUnformatted(Language.FreeCompany);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[0]);
        ImGui.TextUnformatted(player.FreeCompany);
    }

    private void DrawLastSeen(PlayerView player)
    {
        ImGui.TextUnformatted(Language.LastSeen);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[2]);
        ImGui.TextUnformatted(player.LastSeen);
    }

    private void DrawHomeworld(PlayerView player)
    {
        ImGui.TextUnformatted(Language.Homeworld);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[0]);
        if (!string.IsNullOrEmpty(player.PreviousWorlds))
        {
            using (ImRaii.Group())
            {
                ImGui.TextUnformatted(player.HomeWorld);
                ImGui.SameLine();
                using (ImRaii.PushFont(UiBuilder.IconFont))
                    Helper.TextColored(ImGuiColors.DalamudYellow, FontAwesomeIcon.InfoCircle.ToIconString());
            }

            if (ImGui.IsItemHovered())
                Helper.Tooltip(string.Format(Language.PreviouslyOn, player.PreviousWorlds));
        }
        else
        {
            ImGui.TextUnformatted(player.HomeWorld);
        }

        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
    }

    private void DrawFirstSeen(PlayerView player)
    {
        ImGui.TextUnformatted(Language.FirstSeen);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[2]);
        ImGui.TextUnformatted(player.FirstSeen);
    }

    private void DrawName(PlayerView player)
    {
        ImGui.TextUnformatted(Language.Name);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[0]);
        if (!string.IsNullOrEmpty(player.PreviousNames))
        {
            using (ImRaii.Group())
            {
                ImGui.TextUnformatted(player.Name);
                ImGui.SameLine();
                using (ImRaii.PushFont(UiBuilder.IconFont))
                    Helper.TextColored(ImGuiColors.DalamudYellow, FontAwesomeIcon.InfoCircle.ToIconString());
            }

            if (ImGui.IsItemHovered())
                Helper.Tooltip(string.Format(Language.PreviouslyKnownAs, player.PreviousNames));
        }
        else
        {
            ImGui.TextUnformatted(player.Name);
        }

        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
    }

    private void DrawInfoStatHeadings()
    {
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.PlayerInfo);
        ImGuiHelpers.ScaledRelativeSameLine(CurrentOffsets[1]);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.PlayerStats);
    }

    private void CalculateAssignedChildHeight(PlayerView player)
    {
        var itemCount = Math.Max(player.AssignedCategories.Count, player.AssignedTags.Count);
        if (itemCount != 0)
            AssignedChildHeight = (int)Math.Ceiling((double)itemCount / 3) * ImGui.GetTextLineHeightWithSpacing();
        else
            AssignedChildHeight = ImGuiHelpers.GlobalScale * 1f;
    }

    private void DrawCategoryCombo(PlayerView player)
    {
        var categoryNames = player.UnassignedCategories.Select(category => category.Name).Prepend(string.Empty).ToList();
        using var disabled = ImRaii.Disabled(categoryNames.Count == 1);

        if (Helper.Combo("###AddPlayerCategory", ref SelectedCategoryIndex, categoryNames, 160, false))
        {
            if (SelectedCategoryIndex != 0)
            {
                var selectedCategory = ServiceContext.CategoryService.GetCategoryByName(categoryNames[SelectedCategoryIndex]);
                if (selectedCategory != null && player.AssignedCategories.All(category => category.Id != selectedCategory.Id))
                {
                    PlayerCategoryService.AssignCategoryToPlayer(player.Id, selectedCategory.Id);
                    player.AssignedCategories.Add(selectedCategory);
                    player.UnassignedCategories.RemoveAt(SelectedCategoryIndex - 1);
                    SelectedCategoryIndex = 0;
                }
            }
        }
    }

    private void DrawTagCombo(PlayerView player)
    {
        var tagNames = player.UnassignedTags.Select(tag => tag.Name).Prepend(string.Empty).ToList();
        using var disabled = ImRaii.Disabled(tagNames.Count == 1);

        if (Helper.Combo("###AddPlayerTag", ref SelectedTagIndex, tagNames, 160, false))
        {
            if (SelectedTagIndex != 0)
            {
                var selectedTag = ServiceContext.TagService.GetTagByName(tagNames[SelectedTagIndex]);
                if (selectedTag != null && player.AssignedTags.All(tag => tag.Id != selectedTag.Id))
                {
                    PlayerTagService.AssignTag(player.Id, selectedTag.Id);
                    player.AssignedTags.Add(selectedTag);
                    player.UnassignedTags.RemoveAt(SelectedTagIndex - 1);
                    SelectedTagIndex = 0;
                }
            }
        }
    }

    private void DrawAssignedCategories(PlayerView player)
    {
        using var child = ImRaii.Child("AssignedCategories", new Vector2(0, AssignedChildHeight), false);
        if (!child.Success)
            return;

        for (var i = 0; i < player.AssignedCategories.Count; i++)
        {
            var category = player.AssignedCategories[i];
            var colorUint = PlayerConfigService.GetCategoryColor(category);
            var color = Sheets.GetUiColorAsVector4(colorUint);
            var fontColor = Sheets.GetLegibleFontColor(color);

            using (ImRaii.PushColor(ImGuiCol.Text, fontColor).Push(ImGuiCol.Button, color)
                         .Push(ImGuiCol.ButtonActive, color).Push(ImGuiCol.ButtonHovered, color))
            {
                if (category.IsDynamicCategory())
                {
                    ImGui.SmallButton(category.Name);
                }
                else
                {
                    if (ImGui.SmallButton($"{category.Name} x"))
                    {
                        PlayerCategoryService.UnassignCategoryFromPlayer(player.Id, category.Id);
                        player.AssignedCategories.RemoveAll(assignedCategory => assignedCategory.Id == category.Id);
                        player.UnassignedCategories.Add(category);
                    }
                }
            }

            if (ImGui.IsItemHovered())
                Helper.Tooltip(category.IsDynamicCategory() ? Language.UnassignDynamicCategoryTooltip : Language.UnassignCategoryTooltip);

            if ((i + 1) % 3 != 0 && i != player.AssignedCategories.Count - 1)
                ImGui.SameLine();
        }
    }

    private void DrawAssignedTags(PlayerView player)
    {
        using var child = ImRaii.Child("AssignedTags", new Vector2(0, AssignedChildHeight), false);
        if (!child.Success)
            return;

        for (var i = 0; i < player.AssignedTags.Count; i++)
        {
            var tag = player.AssignedTags[i];
            var color = Sheets.GetUiColorAsVector4(tag.Color);
            var fontColor = Sheets.GetLegibleFontColor(color);

            using (ImRaii.PushColor(ImGuiCol.Text, fontColor).Push(ImGuiCol.Button, color)
                         .Push(ImGuiCol.ButtonActive, color).Push(ImGuiCol.ButtonHovered, color))
            {
                if (ImGui.SmallButton($"{tag.Name} x"))
                {
                    PlayerTagService.UnassignTagFromPlayer(player.Id, tag.Id);
                    player.AssignedTags.RemoveAll(assignedTag => assignedTag.Id == tag.Id);
                    player.UnassignedTags.Add(tag);
                }
            }

            if ((i + 1) % 3 != 0 && i != player.AssignedTags.Count - 1)
                ImGui.SameLine();
        }
    }
}
