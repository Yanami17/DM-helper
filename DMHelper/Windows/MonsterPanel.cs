using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace DMHelper.Windows
{
    public class MonsterPanel
    {
        private readonly Plugin plugin;

        private string newMonsterName = string.Empty;
        private int newMonsterHP;
        private int newMonsterDC;

        public MonsterPanel(Plugin plugin)
        {
            this.plugin = plugin;

            newMonsterHP = plugin.Configuration.DefaultMonsterHP;
            newMonsterDC = plugin.Configuration.DefaultMonsterDC;
        }

        public void Draw()
        {
            // ADD MONSTER BUTTON
            if (ImGui.Button("Add Monster"))
            {
                newMonsterName = string.Empty;
                newMonsterHP = plugin.Configuration.DefaultMonsterHP;
                newMonsterDC = plugin.Configuration.DefaultMonsterDC;

                ImGui.OpenPopup("Monster Maker");
            }

            // POPUP MODAL
            if (ImGui.BeginPopup("Monster Maker"))
            {
                ImGui.Text("Create New Monster");
                ImGui.Separator();

                ImGui.Text("Name");
                ImGui.SetNextItemWidth(180);
                ImGui.InputText("##Name", ref newMonsterName, 100);

                ImGui.Text("HP");
                ImGui.SetNextItemWidth(60);
                ImGui.InputInt("##HP", ref newMonsterHP);

                ImGui.Text("DC");
                ImGui.SetNextItemWidth(60);
                ImGui.InputInt("##DC", ref newMonsterDC);

                newMonsterHP = Math.Max(1, newMonsterHP);
                newMonsterDC = Math.Max(1, newMonsterDC);

                ImGui.Spacing();
                ImGui.Separator();

                if (ImGui.Button("Create", new Vector2(100, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(newMonsterName))
                    {
                        plugin.Monsters.Add(new Monster
                        {
                            Name = newMonsterName,
                            MaxHP = newMonsterHP,
                            CurrentHP = newMonsterHP,
                            DC = newMonsterDC,
                            IsEngaged = plugin.Configuration.AutoEngageOnAdd
                        });

                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                    ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }

            ImGui.Separator();
            ImGui.Text("Monsters");

            bool isDefensePhase = plugin.CombatManager.CurrentPhase == CombatManager.CombatPhase.Defense;
            int columnCount = isDefensePhase ? 8 : 7;

            if (ImGui.BeginTable("MonsterTable", columnCount,
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("DC", ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70f);
                ImGui.TableSetupColumn("Engaged", ImGuiTableColumnFlags.WidthFixed, 60f);
                if (isDefensePhase)
                    ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 50f);
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

                    UiHelpers.DrawHpBar(monster.CurrentHP, monster.MaxHP, new Vector2(-1, 0));

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Click to edit HP");

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

                    // STATUS (with targeting indicator)
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

                    // Targeting indicator during Attack Phase
                    if (plugin.CombatManager.CurrentPhase == CombatManager.CombatPhase.Attack)
                    {
                        int targetingCount = plugin.CombatManager.DeclaredTargets
                            .Count(kvp => kvp.Value.Contains(monster.Id));

                        if (targetingCount > 0)
                        {
                            ImGui.SameLine();
                            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.3f, 0.9f, 1f, 1f)))
                                ImGui.TextUnformatted($"×{targetingCount}");

                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip($"{targetingCount} player(s) targeting this monster");
                        }
                    }

                    // ENGAGED
                    ImGui.TableNextColumn();

                    if (monster.CurrentHP > 0)
                    {
                        bool engaged = monster.IsEngaged;
                        if (ImGui.Checkbox("##engaged", ref engaged))
                            monster.IsEngaged = engaged;

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Include in combat resolution");
                    }
                    else
                    {
                        ImGui.TextDisabled("-");
                    }

                    // TARGET COLUMN (Defense Phase only)
                    if (isDefensePhase)
                    {
                        ImGui.TableNextColumn();

                        if (monster.CurrentHP <= 0 || !monster.IsEngaged)
                        {
                            ImGui.TextDisabled("-");
                        }
                        else
                        {
                            DrawMonsterTargetColumn(monster);
                        }
                    }

                    // ACTIONS
                    ImGui.TableNextColumn();

                    if (ImGui.SmallButton("Del"))
                    {
                        plugin.CombatManager.OnMonsterRemoved(monster);
                        plugin.Monsters.RemoveAt(i);
                        ImGui.PopID();
                        break;
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        // =========================================================
        // MONSTER TARGET PICKER (Defense Phase)
        // =========================================================

        private void DrawMonsterTargetColumn(Monster monster)
        {
            var combat = plugin.CombatManager;

            // Build full player list (real + fake)
            var allPlayers = GetAllPlayers();

            if (allPlayers.Count == 0)
            {
                ImGui.TextDisabled("None");
                return;
            }

            combat.MonsterTargets.TryGetValue(monster.Id, out var selectedIds);
            selectedIds ??= new List<uint>();

            // Build summary label
            string label = selectedIds.Count == 0
                ? "None"
                : string.Join(", ", selectedIds
                    .Select(id => allPlayers.FirstOrDefault(p => p.entityId == id).name)
                    .Where(n => n != null));

            if (label.Length > 10)
                label = label.Substring(0, 9) + "…";

            string popupId = $"MonsterTargetPicker_{monster.Id}";

            if (ImGui.SmallButton($"{label}##{monster.Id}"))
                ImGui.OpenPopup(popupId);

            // Tooltip showing full target list
            if (ImGui.IsItemHovered() && selectedIds.Count > 0)
            {
                ImGui.BeginTooltip();
                ImGui.Text("Targeting:");
                foreach (var id in selectedIds)
                {
                    var pName = allPlayers.FirstOrDefault(p => p.entityId == id).name ?? "?";
                    ImGui.BulletText(pName);
                }
                ImGui.EndTooltip();
            }

            if (ImGui.BeginPopup(popupId))
            {
                ImGui.Text($"Targets for {monster.Name}");
                ImGui.Separator();

                foreach (var (entityId, name) in allPlayers)
                {
                    bool isSelected = selectedIds.Contains(entityId);

                    if (ImGui.Checkbox(name, ref isSelected))
                    {
                        if (isSelected)
                        {
                            combat.DeclareMonsterTarget(monster.Id, entityId);
                        }
                        else
                        {
                            if (combat.MonsterTargets.TryGetValue(monster.Id, out var list))
                                list.Remove(entityId);
                        }
                    }
                }

                ImGui.Separator();

                if (ImGui.SmallButton("All"))
                {
                    foreach (var (entityId, _) in allPlayers)
                        combat.DeclareMonsterTarget(monster.Id, entityId);
                }

                ImGui.SameLine();

                if (ImGui.SmallButton("None"))
                {
                    if (combat.MonsterTargets.ContainsKey(monster.Id))
                        combat.MonsterTargets[monster.Id].Clear();
                }

                ImGui.SameLine();

                if (ImGui.SmallButton("Close"))
                    ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }
        }

        // Returns all real party members + fake members as (entityId, displayName) tuples
        private List<(uint entityId, string name)> GetAllPlayers()
        {
            var result = new List<(uint, string)>();

            var partyList = Plugin.PartyList;
            if (partyList != null)
            {
                foreach (var member in partyList)
                {
                    if (member != null)
                        result.Add((member.EntityId, member.Name.TextValue));
                }
            }

            foreach (var fake in plugin.FakePartyMembers)
                result.Add((fake.EntityId, fake.Name));

            return result;
        }
    }
}
