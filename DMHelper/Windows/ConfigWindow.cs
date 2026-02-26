using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace DMHelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin)
        : base("DM Configs")
    {
        this.plugin = plugin;
        this.configuration = plugin.Configuration;

        Flags = ImGuiWindowFlags.NoCollapse;

        Size = new Vector2(400, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // ======================
        // UI SETTINGS
        // ======================
        ImGui.Text("UI");
        ImGui.Separator();

        var initials = configuration.UseInitials;
        if (ImGui.Checkbox("Use Initials In Party List", ref initials))
        {
            configuration.UseInitials = initials;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Show player initials instead of full names in the party table.");

        ImGui.Spacing();

        // ======================
        // COMBAT SETTINGS
        // ======================
        ImGui.Text("Combat");
        ImGui.Separator();

        var autoApply = configuration.AutoApplyDamage;
        if (ImGui.Checkbox("Auto-Apply Damage on Resolve", ref autoApply))
        {
            configuration.AutoApplyDamage = autoApply;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("If off, a confirmation step is shown before HP is deducted after resolving.");

        var clearRolls = configuration.ClearRollsAfterResolve;
        if (ImGui.Checkbox("Clear Rolls After Resolve", ref clearRolls))
        {
            configuration.ClearRollsAfterResolve = clearRolls;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("If off, rolls persist after resolving and must be cleared manually.");

        var clampHp = configuration.ClampHPToZero;
        if (ImGui.Checkbox("Clamp HP To Zero", ref clampHp))
        {
            configuration.ClampHPToZero = clampHp;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Prevents HP from going below zero when damage is applied.");

        ImGui.Spacing();

        // ======================
        // PLAYER DEFAULTS
        // ======================
        ImGui.Text("Player Defaults");
        ImGui.Separator();

        int defaultPlayerHP = configuration.DefaultPlayerHP;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Player HP", ref defaultPlayerHP))
        {
            configuration.DefaultPlayerHP = Math.Max(1, defaultPlayerHP);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("HP assigned when clicking Set on a player with no HP set.");

        int playerDamage = configuration.DefaultPlayerDamage;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Player Damage", ref playerDamage))
        {
            configuration.DefaultPlayerDamage = Math.Max(0, playerDamage);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Damage dealt by players on a successful attack roll.");

        ImGui.Spacing();

        // ======================
        // MONSTER DEFAULTS
        // ======================
        ImGui.Text("Monster Defaults");
        ImGui.Separator();

        int defaultHP = configuration.DefaultMonsterHP;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Monster HP", ref defaultHP))
        {
            configuration.DefaultMonsterHP = Math.Max(1, defaultHP);
            configuration.Save();
        }

        int monsterDamage = configuration.DefaultMonsterDamage;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Monster Damage", ref monsterDamage))
        {
            configuration.DefaultMonsterDamage = Math.Max(0, monsterDamage);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Damage dealt by monsters on a failed defense roll.");

        int defaultDC = configuration.DefaultMonsterDC;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Monster DC", ref defaultDC))
        {
            configuration.DefaultMonsterDC = Math.Max(1, defaultDC);
            configuration.Save();
        }

        var autoEngage = configuration.AutoEngageOnAdd;
        if (ImGui.Checkbox("Auto-Engage Monsters on Add", ref autoEngage))
        {
            configuration.AutoEngageOnAdd = autoEngage;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Newly created monsters are automatically marked as Engaged.");

        ImGui.Spacing();

        // ======================
        // ROLL SETTINGS
        // ======================
        ImGui.Text("Roll Settings");
        ImGui.Separator();

        int maxRoll = configuration.MaxRollValue;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Max Roll Value", ref maxRoll))
        {
            configuration.MaxRollValue = Math.Max(2, maxRoll);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("The maximum value of the die (e.g. 20 for d20). Affects nat roll highlights.");

        ImGui.Spacing();

        // ======================
        // FEED SETTINGS
        // ======================
        ImGui.Text("Combat Feed");
        ImGui.Separator();

        var showBreakdown = configuration.ShowRollBreakdown;
        if (ImGui.Checkbox("Show Roll Breakdown", ref showBreakdown))
        {
            configuration.ShowRollBreakdown = showBreakdown;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Shows [roll vs DC] on each combat feed entry.");

        int feedMax = configuration.FeedMaxEntries;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Max Feed Entries", ref feedMax))
        {
            configuration.FeedMaxEntries = Math.Max(0, feedMax);
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Maximum number of entries in the combat feed. Set to 0 for unlimited.");

        ImGui.Spacing();

        // ======================
        // DEBUG
        // ======================
        ImGui.Text("Debug");
        ImGui.Separator();

        var debugMode = configuration.DebugMode;
        if (ImGui.Checkbox("Enable Debug Mode", ref debugMode))
        {
            configuration.DebugMode = debugMode;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Writes verbose debug messages to /xllog.");
    }
}
