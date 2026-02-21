using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

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

        Size = new Vector2(380, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        // ======================
        // UI SETTINGS
        // ======================
        ImGui.Text("UI Settings");
        ImGui.Separator();

        var initials = configuration.UseInitials;
        if (ImGui.Checkbox("Use Initials In Party List", ref initials))
        {
            configuration.UseInitials = initials;
            configuration.Save();
        }

        ImGui.Spacing();

        // ======================
        // COMBAT SETTINGS
        // ======================
        ImGui.Text("Combat Settings");
        ImGui.Separator();

        int playerDamage = configuration.DefaultPlayerDamage;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Player Damage", ref playerDamage))
        {
            configuration.DefaultPlayerDamage = Math.Max(0, playerDamage);
            configuration.Save();
        }

        ImGui.Spacing();

        var clampHp = configuration.ClampHPToZero;
        if (ImGui.Checkbox("Clamp HP To Zero", ref clampHp))
        {
            configuration.ClampHPToZero = clampHp;
            configuration.Save();
        }

        var confirmDamage = configuration.ConfirmBeforeApplyingDamage;
        if (ImGui.Checkbox("Confirm Before Applying Damage", ref confirmDamage))
        {
            configuration.ConfirmBeforeApplyingDamage = confirmDamage;
            configuration.Save();
        }

        var clearRolls = configuration.ClearRollsAfterResolve;
        if (ImGui.Checkbox("Clear Rolls After Resolve", ref clearRolls))
        {
            configuration.ClearRollsAfterResolve = clearRolls;
            configuration.Save();
        }

        var showBreakdown = configuration.ShowRollBreakdown;
        if (ImGui.Checkbox("Show Roll Breakdown", ref showBreakdown))
        {
            configuration.ShowRollBreakdown = showBreakdown;
            configuration.Save();
        }

        ImGui.Spacing();

        // ======================
        // DEFAULT MONSTER TEMPLATE
        // ======================
        ImGui.Text("Default Monster Template");
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

        int defaultDC = configuration.DefaultMonsterDC;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Default Monster DC", ref defaultDC))
        {
            configuration.DefaultMonsterDC = Math.Max(1, defaultDC);
            configuration.Save();
        }

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
    }
}
