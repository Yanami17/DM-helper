using Dalamud.Configuration;
using System;

namespace DMHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // ── UI ────────────────────────────────────────────────────
    public bool UseInitials { get; set; } = true;

    // ── Combat ───────────────────────────────────────────────
    public int DefaultPlayerDamage { get; set; } = 1;
    public int DefaultMonsterDamage { get; set; } = 1;

    // If true, damage is applied immediately on resolve.
    // If false, the DM gets a confirmation step before HP is deducted.
    public bool AutoApplyDamage { get; set; } = true;

    // If true, rolls are cleared automatically after each resolve.
    // If false, rolls persist until manually cleared.
    public bool ClearRollsAfterResolve { get; set; } = true;

    // If true, [roll vs DC] suffix is shown on each feed entry.
    public bool ShowRollBreakdown { get; set; } = true;

    // If true, HP is never allowed to go below zero.
    public bool ClampHPToZero { get; set; } = true;

    // If true, debug messages are written to xllog.
    public bool DebugMode { get; set; } = false;

    // ── Monster Defaults ─────────────────────────────────────
    public int DefaultMonsterHP { get; set; } = 10;
    public int DefaultMonsterDC { get; set; } = 10;

    // If true, monsters are automatically marked as Engaged when created.
    public bool AutoEngageOnAdd { get; set; } = false;

    // ── Player Defaults ──────────────────────────────────────
    public int DefaultPlayerHP { get; set; } = 10;

    // ── Roll Settings ────────────────────────────────────────
    // The maximum value of the die being rolled (e.g. 20 for d20).
    // Used for nat roll highlights and future roll logic.
    public int MaxRollValue { get; set; } = 20;

    // ── Feed Settings ────────────────────────────────────────
    // Maximum number of entries kept in the combat feed. 0 = unlimited.
    public int FeedMaxEntries { get; set; } = 0;

    // Convenience save method
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
