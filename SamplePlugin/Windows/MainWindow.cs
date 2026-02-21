using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Reflection.Metadata.Ecma335;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    private readonly Dictionary<uint, int> lastRolls = new();

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("DM Tool##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;

        Plugin.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(
        XivChatType type,
        int timestamp,
        ref SeString sender,
        ref SeString message,
        ref bool isHandled)
    {
        if (type != XivChatType.Party)
            return;

        Plugin.Log.Information($"Type: {type}");
        Plugin.Log.Information($"Sender: '{sender.TextValue}'");
        Plugin.Log.Information($"Message: '{message.TextValue}'");

        var text = message.TextValue;

        var match = Regex.Match(text, @"Random!.*?(\d+)$");
        if (!match.Success)
            return;

        int roll = int.Parse(match.Groups[1].Value);

        var partyList = Plugin.PartyList;
        if (partyList == null)
            return;

        string cleanName = sender.TextValue.TrimStart('', '', '', '');

        foreach (var member in partyList)
        {
            if (member == null)
                continue;

            if (member.Name.TextValue.Equals(cleanName, StringComparison.Ordinal))
            {
                lastRolls[member.EntityId] = roll;
                Plugin.Log.Information($"Stored roll {roll} for {cleanName}");
                break;
            }
        }
    }


    public override void Draw()
    {
        ImGui.Text($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("DM Configs"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        // =============================
        // PARTY DISPLAY + CHAT ROLLS
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

        using (var partyChild = ImRaii.Child("PartyContainer", new Vector2(0, 200f), true))
        {
        if (!partyChild.Success)
            return;

        if (ImGui.BeginTable("PartyTable", 4,
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Job / Level", ImGuiTableColumnFlags.WidthFixed, 110f);
            ImGui.TableSetupColumn("Last Roll", ImGuiTableColumnFlags.WidthFixed, 70f);
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

                    // Index
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i + 1}");

                    // Name
                    ImGui.TableNextColumn();
                    if (isLocal)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.3f, 1f, 0.3f, 1f)))
                            ImGui.Text(member.Name.TextValue);
                    }
                    else
                    {
                        ImGui.Text(member.Name.TextValue);
                    }

                    // Job/Level
                    ImGui.TableNextColumn();
                    ImGui.Text($"{member.ClassJob.Value.Abbreviation} {member.Level}");

                    // Last Roll
                    ImGui.TableNextColumn();

                    if (lastRolls.TryGetValue(member.EntityId, out var roll))
                    {
                        if (roll == 20)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.2f, 1f, 0.2f, 1f)))
                                ImGui.Text(roll.ToString());
                        }
                        else if (roll == 1)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.2f, 0.2f, 1f)))
                                ImGui.Text(roll.ToString());
                        }
                        else
                        {
                            ImGui.Text(roll.ToString());
                        }
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                }
                ImGui.EndTable();
                }
            }

        // =============================
        // MONSTER DISPLAY
        // =============================

        ImGui.Separator();
        ImGui.Text("Monsters");

        for (int i = 0; i < plugin.Monsters.Count; i++)
        {
            var monster = plugin.Monsters[i];

            ImGui.PushID(i);

            ImGui.Text($"{monster.Name}");
            ImGui.SameLine();
            ImGui.Text($"HP: {monster.CurrentHP}/{monster.MaxHP}");
            ImGui.SameLine();
            ImGui.Text($"DC: {monster.DC}");

            // Damage Button
            if (ImGui.Button("-5 HP"))
            {
                monster.CurrentHP = Math.Max(0, monster.CurrentHP - 5);
            }

            ImGui.SameLine();

            // Heal Button
            if (ImGui.Button("+5 HP"))
            {
                monster.CurrentHP = Math.Min(monster.MaxHP, monster.CurrentHP + 5);
            }

            ImGui.SameLine();

            // Delete Button
            if (ImGui.Button("Delete"))
            {
                plugin.Monsters.RemoveAt(i);
                ImGui.PopID();
                break;
            }

            ImGui.Separator();
            ImGui.PopID();
        }
    }
}
