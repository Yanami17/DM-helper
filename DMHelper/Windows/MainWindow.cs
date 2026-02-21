using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace DMHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private readonly CombatManager combat;
    private readonly PartyPanel partyPanel;
    private readonly MonsterPanel monsterPanel;


    public MainWindow(Plugin plugin, string goatImagePath)
        : base("DM Tool##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };


        this.plugin = plugin;

        combat = new CombatManager(plugin);
        partyPanel = new PartyPanel(plugin, combat);
        monsterPanel = new MonsterPanel(plugin);
    }

    public void Dispose()
    {
        combat.Dispose();
    }

    // =========================================================
    // UI
    // =========================================================

    public override void Draw()
    {
        if (ImGui.Button("DM Configs"))
            plugin.ToggleConfigUi();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("Combat Phase");

        Vector4 activeColor = new(0.3f, 0.8f, 1f, 1f);
        Vector4 inactiveColor = new(0.5f, 0.5f, 0.5f, 1f);

        using (ImRaii.PushColor(ImGuiCol.Button,
            combat.CurrentPhase == CombatManager.CombatPhase.Attack ? activeColor : inactiveColor))
        {
            if (ImGui.Button("Attack Phase", new Vector2(120, 30)))
                combat.SetPhase(CombatManager.CombatPhase.Attack);
        }

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.Button,
            combat.CurrentPhase == CombatManager.CombatPhase.Defense ? activeColor : inactiveColor))
        {
            if (ImGui.Button("Defense Phase", new Vector2(120, 30)))
                combat.SetPhase(CombatManager.CombatPhase.Defense);
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear Phase", new Vector2(100, 30)))
            combat.SetPhase(CombatManager.CombatPhase.None);


        ImGui.Spacing();

        if (combat.CurrentPhase != CombatManager.CombatPhase.None)
        {
            if (ImGui.Button("Resolve Phase", new Vector2(150, 35)))
                combat.ResolvePhase();

            ImGui.SameLine();

            if (ImGui.Button("Clear Rolls", new Vector2(120, 35)))
                combat.ClearRolls();
        }

        ImGui.Separator();
        partyPanel.Draw();

        ImGui.Separator();
        monsterPanel.Draw();
    }
}
