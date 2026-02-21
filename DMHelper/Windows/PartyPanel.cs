using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;


namespace DMHelper.Windows
{
    public class PartyPanel
    {
        private readonly Plugin plugin;
        private readonly CombatManager combat;

        public PartyPanel(Plugin plugin, CombatManager combat)
        {
            this.plugin = plugin;
            this.combat = combat;
        }

        public void Draw()
        {
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
                        var initials = UiHelpers.GetInitials(fullName);

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
                        if (!combat.PlayerStates.TryGetValue(member.EntityId, out var state))
                        {
                            state = new CombatManager.PlayerState();
                            combat.PlayerStates[member.EntityId] = state;
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

                            UiHelpers.DrawHpBar(state.CurrentHP, state.MaxHP, new Vector2(-1, 0));

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

                        if (combat.LastRolls.TryGetValue(member.EntityId, out var roll))
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
        }
    }

}
