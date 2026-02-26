using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;
using System.Numerics;

namespace DMHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private readonly CombatManager combat;
    private readonly PartyPanel partyPanel;
    private readonly MonsterPanel monsterPanel;
    private readonly DMFeedPanel dmFeed;

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("DM Tool##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        combat = plugin.CombatManager;
        partyPanel = new PartyPanel(plugin, combat);
        monsterPanel = new MonsterPanel(plugin);
        dmFeed = new DMFeedPanel(combat, plugin);
    }

    public void Dispose() { }

    // =========================================================
    // UI
    // =========================================================

    private CombatManager.CombatPhase _pendingPhase = CombatManager.CombatPhase.None;
    private bool _pendingClear = false;

    public override void Draw()
    {
        if (ImGui.Button("DM Configs"))
            plugin.ToggleConfigUi();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("Combat Phase");

        Vector4 activeColor = new(0.3f, 0.8f, 1f, 1f);
        Vector4 inactiveColor = new(0.5f, 0.5f, 0.5f, 1f);
        Vector4 dimColor = new(0.35f, 0.35f, 0.35f, 0.6f);

        using (ImRaii.PushColor(ImGuiCol.Button,
            combat.CurrentPhase == CombatManager.CombatPhase.Attack ? activeColor : inactiveColor))
        {
            if (ImGui.Button("Attack Phase", new Vector2(120, 30)))
            {
                if (combat.HasPendingRolls && combat.CurrentPhase != CombatManager.CombatPhase.Attack)
                {
                    _pendingPhase = CombatManager.CombatPhase.Attack;
                    ImGui.OpenPopup("ConfirmPhaseSwitch");
                }
                else
                {
                    combat.SetPhase(CombatManager.CombatPhase.Attack);
                }
            }
        }

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.Button,
            combat.CurrentPhase == CombatManager.CombatPhase.Defense ? activeColor : inactiveColor))
        {
            if (ImGui.Button("Defense Phase", new Vector2(120, 30)))
            {
                if (combat.HasPendingRolls && combat.CurrentPhase != CombatManager.CombatPhase.Defense)
                {
                    _pendingPhase = CombatManager.CombatPhase.Defense;
                    ImGui.OpenPopup("ConfirmPhaseSwitch");
                }
                else
                {
                    combat.SetPhase(CombatManager.CombatPhase.Defense);
                }
            }
        }

        ImGui.SameLine();

        bool hasPhase = combat.CurrentPhase != CombatManager.CombatPhase.None;

        using (ImRaii.PushColor(ImGuiCol.Button, hasPhase ? inactiveColor : dimColor))
        using (ImRaii.PushColor(ImGuiCol.Text, hasPhase ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(1f, 1f, 1f, 0.4f)))
        {
            if (ImGui.Button("Clear Phase", new Vector2(100, 30)) && hasPhase)
            {
                if (combat.HasPendingRolls)
                {
                    _pendingClear = true;
                    ImGui.OpenPopup("ConfirmPhaseSwitch");
                }
                else
                {
                    combat.SetPhase(CombatManager.CombatPhase.None);
                }
            }
        }

        // ── Discard rolls confirmation popup ──────────────────
        if (ImGui.BeginPopupModal("ConfirmPhaseSwitch", ImGuiWindowFlags.AlwaysAutoResize))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.3f, 1f)))
                ImGui.TextUnformatted("⚠  Unresolved rolls will be discarded.");

            ImGui.Spacing();
            ImGui.TextUnformatted("Are you sure you want to continue?");
            ImGui.Spacing();

            if (ImGui.Button("Discard & Continue", new Vector2(150, 0)))
            {
                if (_pendingClear)
                    combat.SetPhase(CombatManager.CombatPhase.None);
                else
                    combat.SetPhase(_pendingPhase);

                _pendingClear = false;
                _pendingPhase = CombatManager.CombatPhase.None;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(80, 0)))
            {
                _pendingClear = false;
                _pendingPhase = CombatManager.CombatPhase.None;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        // ── Contextual hint ───────────────────────────────────
        ImGui.Spacing();

        if (combat.CurrentPhase == CombatManager.CombatPhase.None)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f)))
                ImGui.TextUnformatted("Select a phase to begin a round.");
        }
        else if (combat.CurrentPhase == CombatManager.CombatPhase.Attack)
        {
            int rolled = CountRolled();
            int total = CountActivePlayers();
            int targeted = CountWithTargets();
            int engaged = CountEngagedMonsters();

            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 1f, 1f)))
                ImGui.TextUnformatted($"Attack Phase — {rolled}/{total} rolled · {targeted}/{engaged} monsters targeted. Assign targets, then resolve.");
        }
        else if (combat.CurrentPhase == CombatManager.CombatPhase.Defense)
        {
            int rolled = CountRolled();
            int total = CountActivePlayers();
            int targeted = CountMonstersWithTargets();

            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.85f, 0.5f, 1f)))
                ImGui.TextUnformatted($"Defense Phase — {rolled}/{total} rolled · {targeted} monster(s) with targets. Resolve when ready.");
        }

        ImGui.Spacing();

        // ── Resolve / Clear Rolls ─────────────────────────────
        if (hasPhase)
        {
            bool hasRolls = combat.AttackRolls.Count > 0 || combat.DefenseRolls.Count > 0;

            // Resolve guard — dim and block if nobody has rolled
            using (ImRaii.PushColor(ImGuiCol.Button, hasRolls ? new Vector4(0.2f, 0.6f, 0.2f, 1f) : dimColor))
            using (ImRaii.PushColor(ImGuiCol.Text, hasRolls ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(1f, 1f, 1f, 0.4f)))
            {
                if (ImGui.Button("Resolve Phase", new Vector2(150, 35)))
                {
                    if (hasRolls)
                        combat.ResolvePhase();
                    else
                        combat.CombatLog.Add(new Combat.CombatLogEntry
                        {
                            Phase = combat.CurrentPhase == CombatManager.CombatPhase.Attack ? "Attack" : "Defense",
                            Phrase = "No rolls to resolve.",
                            Actor = string.Empty,
                            Target = string.Empty
                        });
                }
            }

            if (ImGui.IsItemHovered() && !hasRolls)
                ImGui.SetTooltip("No rolls have been captured yet.");

            ImGui.SameLine();

            if (ImGui.Button("Clear Rolls", new Vector2(120, 35)))
                combat.ClearRolls();
        }

        ImGui.Separator();
        partyPanel.Draw();

        ImGui.Separator();
        monsterPanel.Draw();

        ImGui.Separator();
        dmFeed.Draw();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private int CountActivePlayers()
    {
        int count = 0;
        var partyList = Plugin.PartyList;
        if (partyList != null)
            count += partyList.Count(m => m != null);
        count += plugin.FakePartyMembers.Count;
        return count;
    }

    private int CountRolled()
    {
        if (combat.CurrentPhase == CombatManager.CombatPhase.Attack)
            return combat.AttackRolls.Count;
        if (combat.CurrentPhase == CombatManager.CombatPhase.Defense)
            return combat.DefenseRolls.Count;
        return 0;
    }

    private int CountWithTargets()
    {
        return plugin.Monsters.Count(m =>
            m.IsEngaged && m.CurrentHP > 0 &&
            combat.DeclaredTargets.Any(kvp => kvp.Value.Contains(m.Id)));
    }

    private int CountEngagedMonsters()
    {
        return plugin.Monsters.Count(m => m.IsEngaged && m.CurrentHP > 0);
    }

    private int CountMonstersWithTargets()
    {
        return combat.MonsterTargets.Count(kvp => kvp.Value.Count > 0);
    }
}
