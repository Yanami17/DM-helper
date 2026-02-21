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
    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpper();

        return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
    }

    private class PlayerState
    {
        public int MaxHP = 0;
        public int CurrentHP = 0;

        public string Status = "";
    }

    private readonly Dictionary<uint, PlayerState> playerStates = new();


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

        using (var partyChild = ImRaii.Child("PartyContainer", new Vector2(0, 200f), false))
        {
        if (!partyChild.Success)
            return;

        if (ImGui.BeginTable("PartyTable", 5,
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 35f);
            ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70f);
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

                    // NAME COLUMN
                    ImGui.TableNextColumn();

                    var fullName = member.Name.TextValue;
                    var initials = GetInitials(fullName);

                    if (isLocal)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.3f, 1f, 0.3f, 1f)))
                            ImGui.Text(initials);
                    }
                    else
                    {
                        ImGui.Text(initials);
                    }

                    // Tooltip on hover
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(fullName);
                        ImGui.EndTooltip();
                    }

                    // HP COLUMN
                    ImGui.TableNextColumn();

                    // Ensure state exists FIRST
                    if (!playerStates.TryGetValue(member.EntityId, out var state))
                    {
                        state = new PlayerState();
                        playerStates[member.EntityId] = state;
                    }

                    if (state.MaxHP <= 0)
                    {
                        ImGui.TextDisabled("Unset");
                        ImGui.SameLine();

                        if (ImGui.SmallButton($"Set##{member.EntityId}"))
                        {
                            state.MaxHP = 10;       // Default starting value (change if desired)
                            state.CurrentHP = 10;
                        }
                    }
                    else
                    {
                        // Clamp values safely
                        state.MaxHP = Math.Max(1, state.MaxHP);
                        state.CurrentHP = Math.Clamp(state.CurrentHP, 0, state.MaxHP);

                        float hpPercent = (float)state.CurrentHP / state.MaxHP;

                        // Determine color
                        Vector4 hpColor;

                        if (hpPercent > 0.6f)
                            hpColor = new Vector4(0.2f, 0.8f, 0.2f, 1f);
                        else if (hpPercent > 0.3f)
                            hpColor = new Vector4(0.9f, 0.7f, 0.2f, 1f);
                        else
                            hpColor = new Vector4(0.9f, 0.2f, 0.2f, 1f);

                        using (ImRaii.PushColor(ImGuiCol.PlotHistogram, hpColor))
                        {
                            ImGui.ProgressBar(
                                hpPercent,
                                new Vector2(-1, 0),
                                $"{state.CurrentHP}/{state.MaxHP}"
                            );
                        }

                        // Click HP bar to edit
                        if (ImGui.IsItemClicked())
                        {
                            ImGui.OpenPopup($"EditHP_{member.EntityId}");
                        }

                        if (ImGui.BeginPopup($"EditHP_{member.EntityId}"))
                        {
                            ImGui.InputInt("Max HP", ref state.MaxHP);
                            ImGui.InputInt("Current HP", ref state.CurrentHP);

                            state.MaxHP = Math.Max(1, state.MaxHP);
                            state.CurrentHP = Math.Clamp(state.CurrentHP, 0, state.MaxHP);

                            if (ImGui.Button("Full Heal"))
                                state.CurrentHP = state.MaxHP;

                            ImGui.SameLine();

                            if (ImGui.Button("Close"))
                                ImGui.CloseCurrentPopup();

                            ImGui.EndPopup();
                        }
                    }

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
                    // STATUS COLUMN
                    ImGui.TableNextColumn();

                    // state already exists from HP column logic

                    if (state.MaxHP <= 0)
                    {
                        ImGui.Text("-");
                    }
                    else if (state.CurrentHP == 0)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f)))
                            ImGui.Text("Downed");
                    }
                    else if (!string.IsNullOrEmpty(state.Status))
                    {
                        if (state.Status == "Hit" || state.Status == "Defended")
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.2f, 1f, 0.2f, 1f)))
                                ImGui.Text(state.Status);
                        }
                        else if (state.Status == "Miss" || state.Status == "Failed")
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.2f, 0.2f, 1f)))
                                ImGui.Text(state.Status);
                        }
                        else
                        {
                            ImGui.Text(state.Status);
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

        if (ImGui.BeginTable("MonsterTable", 6,
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("DC", ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableHeadersRow();

            for (int i = 0; i < plugin.Monsters.Count; i++)
            {
                var monster = plugin.Monsters[i];

                ImGui.TableNextRow();
                ImGui.PushID(i);

                // INDEX
                ImGui.TableNextColumn();
                ImGui.Text($"{i + 1}");

                // NAME
                ImGui.TableNextColumn();
                ImGui.Text(monster.Name);

                // HP
                ImGui.TableNextColumn();

                monster.MaxHP = Math.Max(1, monster.MaxHP);
                monster.CurrentHP = Math.Clamp(monster.CurrentHP, 0, monster.MaxHP);

                float hpPercent = (float)monster.CurrentHP / monster.MaxHP;

                Vector4 hpColor;

                if (hpPercent > 0.6f)
                    hpColor = new Vector4(0.2f, 0.8f, 0.2f, 1f);
                else if (hpPercent > 0.3f)
                    hpColor = new Vector4(0.9f, 0.7f, 0.2f, 1f);
                else
                    hpColor = new Vector4(0.9f, 0.2f, 0.2f, 1f);

                using (ImRaii.PushColor(ImGuiCol.PlotHistogram, hpColor))
                {
                    ImGui.ProgressBar(
                        hpPercent,
                        new Vector2(-1, 0),
                        $"{monster.CurrentHP}/{monster.MaxHP}"
                    );
                }

                if (ImGui.IsItemClicked())
                    ImGui.OpenPopup($"EditMonsterHP_{i}");

                if (ImGui.BeginPopup($"EditMonsterHP_{i}"))
                {
                    int maxHp = monster.MaxHP;
                    int currentHp = monster.CurrentHP;

                    if (ImGui.InputInt("Max HP", ref maxHp))
                        monster.MaxHP = maxHp;

                    if (ImGui.InputInt("Current HP", ref currentHp))
                        monster.CurrentHP = currentHp;

                    monster.MaxHP = Math.Max(1, monster.MaxHP);
                    monster.CurrentHP = Math.Clamp(monster.CurrentHP, 0, monster.MaxHP);

                    if (ImGui.Button("Full Heal"))
                        monster.CurrentHP = monster.MaxHP;

                    ImGui.SameLine();

                    if (ImGui.Button("Close"))
                        ImGui.CloseCurrentPopup();

                    ImGui.EndPopup();
                }


                // DC
                ImGui.TableNextColumn();
                ImGui.Text(monster.DC.ToString());

                // STATUS
                ImGui.TableNextColumn();

                if (monster.CurrentHP == 0)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f)))
                        ImGui.Text("Defeated");
                }
                else
                {
                    ImGui.Text("-");
                }

                // ACTIONS
                ImGui.TableNextColumn();

                if (ImGui.SmallButton("Del"))
                {
                    plugin.Monsters.RemoveAt(i);
                    ImGui.PopID();
                    break;
                }

                ImGui.PopID();
            }
            ImGui.EndTable();
        }
    }
}
