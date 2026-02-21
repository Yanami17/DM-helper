using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    private readonly Dictionary<uint, int> lastRolls = new();
    private readonly Random rng = new();

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                // Example for other services that Dalamud provides.
                // PlayerState provides a wrapper filled with information about the player character.

                var playerState = Plugin.PlayerState;
                if (!playerState.IsLoaded)
                {
                    ImGui.Text("Our local player is currently not logged in.");
                    return;
                }

                if (!playerState.ClassJob.IsValid)
                {
                    ImGui.Text("Our current job is currently not valid.");
                    return;
                }

                // If you want to see the Macro representation of this SeString use `.ToMacroString()`
                // More info about SeStrings: https://dalamud.dev/plugin-development/sestring/
                ImGui.Text($"Our current job is ({playerState.ClassJob.RowId}) '{playerState.ClassJob.Value.Abbreviation}' with level {playerState.Level}");

                // Example for querying Lumina, getting the name of our current area.
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.Text($"We are currently in ({territoryId}) '{territoryRow.PlaceName.Value.Name}'");
                }
                else
                {
                    ImGui.Text("Invalid territory.");
                }

                // =============================
                // PARTY DISPLAY (DROP-IN BLOCK)
                // =============================

                ImGuiHelpers.ScaledDummy(15.0f);
                ImGui.Separator();
                ImGui.Text("Current Party");

                var partyList = Plugin.PartyList;

                if (partyList == null || partyList.Length == 0)
                {
                    ImGui.Text("Not currently in a party.");
                    return;
                }

                float rowHeight = ImGui.GetTextLineHeightWithSpacing();
                float tableHeight = rowHeight * partyList.Length + 30f;

                using (var partyChild = ImRaii.Child("PartyContainer", new Vector2(0, 200f), true))
                {
                    if (!partyChild.Success)
                        return;

                    if (ImGui.BeginTable("PartyTable", 3,
                        ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.Borders |
                        ImGuiTableFlags.SizingStretchProp))
                    {
                        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
                        ImGui.TableSetupColumn("Name");
                        ImGui.TableSetupColumn("Job / Level", ImGuiTableColumnFlags.WidthFixed, 110f);
                        ImGui.TableHeadersRow();

                        for (int i = 0; i < partyList.Length; i++)
                        {
                            var member = partyList[i];
                            if (member == null)
                                continue;

                            ImGui.TableNextRow();

                            bool isLocal =
                                Plugin.ObjectTable.LocalPlayer != null &&
                                member.EntityId == Plugin.ObjectTable.LocalPlayer.EntityId;

                            ImGui.TableNextColumn();
                            ImGui.Text($"{i + 1}");

                            ImGui.TableNextColumn();
                            if (isLocal)
                            {
                                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.3f, 1f, 0.3f, 1f)))
                                {
                                    ImGui.Text($"{member.Name.TextValue} (You)");
                                }
                            }
                            else
                            {
                                ImGui.Text(member.Name.TextValue);
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text($"{member.ClassJob.Value.Abbreviation} {member.Level}");
                        }

                        ImGui.EndTable();
                    }
                }
            }
        }
    }
}
