using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;


namespace SamplePlugin.Windows
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
                // Reset to defaults every time popup opens
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
                            DC = newMonsterDC
                        });

                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.Separator();


            ImGui.Text("Monsters");

            if (ImGui.BeginTable("MonsterTable", 7,
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 90f);
                ImGui.TableSetupColumn("DC", ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 50f);
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

                    UiHelpers.DrawHpBar(monster.CurrentHP, monster.MaxHP, new Vector2(-1, 0));

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

                    // ENGAGED COLUMN (before Status)
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

}
