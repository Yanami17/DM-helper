using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;


namespace DMHelper.Windows
{
    public class PartyPanel
    {
        private readonly Plugin plugin;
        private readonly CombatManager combat;

        // Buffer for new fake member name input
        private string newFakeMemberName = "Fake Member";

        public PartyPanel(Plugin plugin, CombatManager combat)
        {
            this.plugin = plugin;
            this.combat = combat;
        }

        public void Draw()
        {
            DrawRealParty();

            ImGui.Spacing();

            DrawFakeParty();
        }

        // =========================================================
        // REAL PARTY
        // =========================================================

        private void DrawRealParty()
        {
            ImGui.Text("Current Party");

            var partyList = Plugin.PartyList;

            if (partyList == null || partyList.Length == 0)
            {
                ImGui.TextDisabled("Not currently in a party.");
            }
            else
            {
                bool isAttackPhase = combat.CurrentPhase == CombatManager.CombatPhase.Attack;
                int columnCount = isAttackPhase ? 6 : 5;

                using (var partyChild = ImRaii.Child("PartyContainer", new Vector2(0, 200f), false))
                {
                    if (!partyChild.Success)
                        return;

                    if (ImGui.BeginTable("PartyTable", columnCount,
                        ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.Borders |
                        ImGuiTableFlags.SizingStretchProp))
                    {
                        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, plugin.Configuration.UseInitials ? 35f : 110f);
                        ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
                        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 50f);
                        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70f);
                        if (isAttackPhase)
                            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 90f);
                        ImGui.TableHeadersRow();

                        for (int i = 0; i < partyList.Length; i++)
                        {
                            var member = partyList[i];
                            if (member == null) continue;

                            ImGui.TableNextRow();
                            ImGui.PushID(i);

                            // Highlight row if this player has already rolled this phase
                            if (combat.CurrentPhase != CombatManager.CombatPhase.None && combat.HasRolled(member.EntityId))
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.4f, 0.15f, 0.4f)));
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.4f, 0.15f, 0.55f)));
                            }

                            bool isLocal =
                                Plugin.ObjectTable.LocalPlayer != null &&
                                member.EntityId == Plugin.ObjectTable.LocalPlayer.EntityId;

                            DrawIndexColumn(i + 1);
                            DrawNameColumn(member.Name.TextValue, isLocal);
                            DrawHpColumn(member.EntityId);
                            DrawRollColumn(member.EntityId);
                            DrawStatusColumn(member.EntityId);
                            if (isAttackPhase)
                                DrawTargetColumn(member.EntityId, member.Name.TextValue);

                            ImGui.PopID();
                        }

                        ImGui.EndTable();
                    }
                }
            }
        }

        // =========================================================
        // FAKE PARTY
        // =========================================================

        private void DrawFakeParty()
        {
            ImGui.Text("Fake Party (Testing)");
            ImGui.SameLine();

            if (ImGui.SmallButton("+ Add"))
                ImGui.OpenPopup("AddFakeMember");

            if (ImGui.BeginPopup("AddFakeMember"))
            {
                ImGui.Text("New Fake Member");
                ImGui.Separator();
                ImGui.SetNextItemWidth(160f);
                ImGui.InputText("Name##fake", ref newFakeMemberName, 64);

                ImGui.Spacing();

                if (ImGui.Button("Add", new Vector2(80, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(newFakeMemberName))
                    {
                        var fake = new FakePartyMember { Name = newFakeMemberName };
                        plugin.FakePartyMembers.Add(fake);

                        // Initialise their PlayerState
                        combat.PlayerStates[fake.EntityId] = new CombatManager.PlayerState
                        {
                            MaxHP = 10,
                            CurrentHP = 10
                        };

                        newFakeMemberName = "Fake Member";
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                    ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }

            if (plugin.FakePartyMembers.Count == 0)
            {
                ImGui.TextDisabled("No fake members added.");
                return;
            }

            bool isAttackPhase = combat.CurrentPhase == CombatManager.CombatPhase.Attack;
            int columnCount = isAttackPhase ? 7 : 6;

            if (ImGui.BeginTable("FakePartyTable", columnCount,
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70f);
                ImGui.TableSetupColumn("Say", ImGuiTableColumnFlags.WidthFixed, 60f);
                if (isAttackPhase)
                    ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < plugin.FakePartyMembers.Count; i++)
                {
                    var fake = plugin.FakePartyMembers[i];

                    ImGui.TableNextRow();
                    ImGui.PushID($"fake_{i}");

                    // Highlight row if this fake member has already rolled this phase
                    if (combat.CurrentPhase != CombatManager.CombatPhase.None && combat.HasRolled(fake.EntityId))
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.4f, 0.15f, 0.4f)));
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.4f, 0.15f, 0.55f)));
                    }

                    // INDEX
                    ImGui.TableNextColumn();
                    ImGui.Text($"F{i + 1}");

                    // NAME (editable inline)
                    ImGui.TableNextColumn();
                    string name = fake.Name;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText("##fakename", ref name, 64))
                        fake.Name = name;

                    // HP
                    DrawHpColumn(fake.EntityId);

                    // ROLL — manual button if not listening on say
                    ImGui.TableNextColumn();
                    {
                        int? roll = null;

                        if (isAttackPhase)
                        {
                            if (combat.AttackRolls.TryGetValue(fake.EntityId, out var monsterRolls)
                                && monsterRolls.TryGetValue(Guid.Empty, out int storedRoll))
                                roll = storedRoll;
                        }
                        else if (combat.CurrentPhase == CombatManager.CombatPhase.Defense)
                        {
                            if (combat.DefenseRolls.TryGetValue(fake.EntityId, out var defRoll))
                                roll = defRoll;
                        }

                        if (combat.CurrentPhase != CombatManager.CombatPhase.None)
                        {
                            // Show roll value, with a small re-roll button if not using say
                            if (!fake.ListenOnSay)
                            {
                                if (ImGui.SmallButton($"Roll##fr_{i}"))
                                    combat.RollForFakeMember(fake);

                                if (roll.HasValue)
                                {
                                    ImGui.SameLine();
                                    DrawRollValue(roll.Value);
                                }
                            }
                            else
                            {
                                // Listening on say — just show current roll if any
                                if (roll.HasValue)
                                    DrawRollValue(roll.Value);
                                else
                                    ImGui.TextDisabled("…");
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled("-");
                        }
                    }

                    // STATUS
                    DrawStatusColumn(fake.EntityId);

                    // SAY TOGGLE
                    ImGui.TableNextColumn();
                    {
                        bool listenOnSay = fake.ListenOnSay;

                        // Highlight the cell when active
                        if (listenOnSay)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg,
                                ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.5f, 0.2f, 0.5f)));
                        }

                        if (ImGui.Checkbox($"##say_{fake.EntityId}", ref listenOnSay))
                        {
                            if (listenOnSay)
                            {
                                foreach (var other in plugin.FakePartyMembers)
                                    other.ListenOnSay = false;
                            }
                            fake.ListenOnSay = listenOnSay;
                        }

                        ImGui.SameLine();

                        using (ImRaii.PushColor(ImGuiCol.Text,
                            listenOnSay
                                ? new Vector4(0.3f, 1f, 0.3f, 1f)
                                : new Vector4(0.5f, 0.5f, 0.5f, 1f)))
                        {
                            ImGui.TextUnformatted(listenOnSay ? "ON" : "OFF");
                        }

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(listenOnSay
                                ? "Listening on /say — your rolls go here"
                                : "Click to route /say rolls here");
                    }

                    // TARGET (attack phase only)
                    if (isAttackPhase)
                        DrawTargetColumn(fake.EntityId, fake.Name);

                    // DELETE button — tucked into the name column via context menu
                    if (ImGui.BeginPopupContextItem($"fakecontext_{i}"))
                    {
                        if (ImGui.MenuItem("Remove"))
                        {
                            plugin.FakePartyMembers.RemoveAt(i);
                            combat.PlayerStates.Remove(fake.EntityId);
                            ImGui.EndPopup();
                            ImGui.PopID();
                            break;
                        }
                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.TextDisabled("Right-click a row to remove a fake member.");
        }

        // =========================================================
        // SHARED COLUMN HELPERS
        // =========================================================

        private void DrawIndexColumn(int index)
        {
            ImGui.TableNextColumn();
            ImGui.Text($"{index}");
        }

        private void DrawNameColumn(string fullName, bool isLocal)
        {
            ImGui.TableNextColumn();

            bool useInitials = plugin.Configuration.UseInitials;
            string display = useInitials ? UiHelpers.GetInitials(fullName) : fullName;

            if (isLocal)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.3f, 1f, 0.3f, 1f)))
                    ImGui.Text(display);
            }
            else
            {
                ImGui.Text(display);
            }

            // Always show full name on hover regardless of display mode
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(fullName);
                ImGui.EndTooltip();
            }
        }

        private void DrawHpColumn(uint entityId)
        {
            ImGui.TableNextColumn();

            if (!combat.PlayerStates.TryGetValue(entityId, out var state))
            {
                state = new CombatManager.PlayerState();
                combat.PlayerStates[entityId] = state;
            }

            if (state.MaxHP <= 0)
            {
                ImGui.TextDisabled("Unset");
                ImGui.SameLine();

                if (ImGui.SmallButton($"Set##{entityId}"))
                {
                    state.MaxHP = plugin.Configuration.DefaultPlayerHP;
                    state.CurrentHP = plugin.Configuration.DefaultPlayerHP;
                }
            }
            else
            {
                state.MaxHP = Math.Max(1, state.MaxHP);
                state.CurrentHP = Math.Clamp(state.CurrentHP, 0, state.MaxHP);

                UiHelpers.DrawHpBar(state.CurrentHP, state.MaxHP, new Vector2(-1, 0));

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Click to edit HP");

                if (ImGui.IsItemClicked())
                    ImGui.OpenPopup($"EditHP_{entityId}");

                if (ImGui.BeginPopup($"EditHP_{entityId}"))
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
        }

        private void DrawRollColumn(uint entityId)
        {
            ImGui.TableNextColumn();

            int? roll = null;

            if (combat.CurrentPhase == CombatManager.CombatPhase.Attack)
            {
                if (combat.AttackRolls.TryGetValue(entityId, out var monsterRolls)
                    && monsterRolls.TryGetValue(Guid.Empty, out int storedRoll))
                    roll = storedRoll;
            }
            else if (combat.CurrentPhase == CombatManager.CombatPhase.Defense)
            {
                if (combat.DefenseRolls.TryGetValue(entityId, out var defRoll))
                    roll = defRoll;
            }

            if (roll.HasValue)
                DrawRollValue(roll.Value);
            else
                ImGui.Text("-");
        }

        private void DrawRollValue(int roll)
        {
            int max = plugin.Configuration.MaxRollValue;

            if (roll == max)
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

        private void DrawStatusColumn(uint entityId)
        {
            ImGui.TableNextColumn();

            if (!combat.PlayerStates.TryGetValue(entityId, out var state))
            {
                ImGui.Text("-");
                return;
            }

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

            // During Defense Phase show which monsters are targeting this player
            if (combat.CurrentPhase == CombatManager.CombatPhase.Defense)
            {
                var incomingMonsters = plugin.Monsters
                    .Where(m => m.IsEngaged && m.CurrentHP > 0
                        && combat.MonsterTargets.TryGetValue(m.Id, out var targets)
                        && targets.Contains(entityId))
                    .ToList();

                if (incomingMonsters.Count > 0)
                {
                    ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.5f, 0.2f, 1f)))
                        ImGui.TextUnformatted($"⚔{incomingMonsters.Count}");

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted("Targeted by:");
                        foreach (var m in incomingMonsters)
                            ImGui.BulletText(m.Name);
                        ImGui.EndTooltip();
                    }
                }
            }
        }

        private void DrawTargetColumn(uint entityId, string displayName)
        {
            ImGui.TableNextColumn();

            var engagedMonsters = plugin.Monsters
                .Where(m => m.IsEngaged && m.CurrentHP > 0)
                .ToList();

            if (engagedMonsters.Count == 0)
            {
                ImGui.TextDisabled("None");
                return;
            }

            combat.DeclaredTargets.TryGetValue(entityId, out var selectedIds);
            selectedIds ??= new List<Guid>();

            string label = selectedIds.Count == 0
                ? "None"
                : string.Join(", ", selectedIds
                    .Select(id => engagedMonsters.FirstOrDefault(m => m.Id == id)?.Name)
                    .Where(n => n != null));

            if (label.Length > 10)
                label = label.Substring(0, 9) + "…";

            string popupId = $"TargetPicker_{entityId}";

            if (ImGui.SmallButton($"{label}##{entityId}"))
                ImGui.OpenPopup(popupId);

            if (ImGui.IsItemHovered() && selectedIds.Count > 0)
            {
                ImGui.BeginTooltip();
                ImGui.Text("Targets:");
                foreach (var id in selectedIds)
                {
                    var name = engagedMonsters.FirstOrDefault(m => m.Id == id)?.Name ?? "?";
                    ImGui.BulletText(name);
                }
                ImGui.EndTooltip();
            }

            if (ImGui.BeginPopup(popupId))
            {
                ImGui.Text($"Targets for {UiHelpers.GetInitials(displayName)}");
                ImGui.Separator();

                foreach (var monster in engagedMonsters)
                {
                    bool isSelected = selectedIds.Contains(monster.Id);

                    if (ImGui.Checkbox(monster.Name, ref isSelected))
                    {
                        if (isSelected)
                            combat.DeclareTarget(entityId, monster.Id);
                        else
                        {
                            if (combat.DeclaredTargets.TryGetValue(entityId, out var list))
                                list.Remove(monster.Id);
                        }
                    }
                }

                ImGui.Separator();

                if (ImGui.SmallButton("All"))
                {
                    foreach (var monster in engagedMonsters)
                        combat.DeclareTarget(entityId, monster.Id);
                }

                ImGui.SameLine();

                if (ImGui.SmallButton("None"))
                {
                    if (combat.DeclaredTargets.ContainsKey(entityId))
                        combat.DeclaredTargets[entityId].Clear();
                }

                ImGui.SameLine();

                if (ImGui.SmallButton("Close"))
                    ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }
        }
    }
}
