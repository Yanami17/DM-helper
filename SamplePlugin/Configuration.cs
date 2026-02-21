using Dalamud.Configuration;
using System;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    // UI
    public bool UseInitials { get; set; } = true;
    public bool UseCompactView { get; set; } = false;

    // Combat
    public int DefaultPlayerDamage { get; set; } = 1;
    public int DefaultMonsterDamage { get; set; } = 1;
    public bool AutoApplyDamage { get; set; } = true;

    // Monster Defaults
    public int DefaultMonsterHP { get; set; } = 10;
    public int DefaultMonsterDC { get; set; } = 10;

    public bool ClampHPToZero = true;
    public bool ClearRollsAfterResolve = true;
    public bool ConfirmBeforeApplyingDamage = false;
    public bool ShowRollBreakdown = true;
    public bool DebugMode = false;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
