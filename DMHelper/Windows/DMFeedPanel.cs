using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using DMHelper.Combat;
using System.Linq;
using System.Numerics;

namespace DMHelper.Windows;

public class DMFeedPanel
{
    private readonly CombatManager combat;

    private readonly Plugin plugin;
    private int _lastLogCount = 0;
    private bool _hasUnseenEntries = false;
    private string _lastRenderedPhase = string.Empty;

    public DMFeedPanel(CombatManager combat, Plugin plugin)
    {
        this.combat = combat;
        this.plugin = plugin;
    }

    public void Draw()
    {
        ImGui.Text("Combat Feed");

        ImGui.SameLine();

        if (ImGui.SmallButton("Clear"))
        {
            combat.CombatLog.Clear();
            _lastRenderedPhase = string.Empty;
            _lastLogCount = 0;
            _hasUnseenEntries = false;
        }

        // ── Confirm damage button (shown when AutoApplyDamage is off and damage is pending) ──
        bool hasPendingDamage = plugin.Monsters.Any(m => m.PendingDamage > 0);
        if (!plugin.Configuration.AutoApplyDamage && hasPendingDamage)
        {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.7f, 0.3f, 0.2f, 1f)))
            {
                if (ImGui.SmallButton("Apply Damage"))
                    combat.ApplyPendingDamage();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Confirm and apply pending damage to monsters.");
        }

        // ── New entries indicator ─────────────────────────────
        if (_hasUnseenEntries)
        {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.3f, 0.6f, 0.3f, 1f)))
            {
                if (ImGui.SmallButton("↓ New entries"))
                    _hasUnseenEntries = false;
            }
        }

        ImGui.Separator();

        // Enforce feed cap
        int maxEntries = plugin.Configuration.FeedMaxEntries;
        if (maxEntries > 0 && combat.CombatLog.Count > maxEntries)
            combat.CombatLog.RemoveRange(0, combat.CombatLog.Count - maxEntries);

        using var child = ImRaii.Child("CombatLogChild", Vector2.Zero, false,
            ImGuiWindowFlags.HorizontalScrollbar);

        if (!child.Success)
            return;

        bool isAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 20f;

        if (combat.CombatLog.Count > _lastLogCount)
        {
            if (!isAtBottom)
                _hasUnseenEntries = true;

            _lastLogCount = combat.CombatLog.Count;
        }

        string currentPhase = string.Empty;

        for (int i = 0; i < combat.CombatLog.Count; i++)
        {
            var entry = combat.CombatLog[i];

            if (entry.Phase != currentPhase
                && entry.Phase is not "MonsterDefeated" and not "PlayerDowned")
            {
                if (i > 0)
                    ImGui.Spacing();

                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.6f, 0.85f, 1f, 1f)))
                    ImGui.TextUnformatted($"── {entry.Phase.ToUpper()} PHASE ──────────────────");

                ImGui.Spacing();
                currentPhase = entry.Phase;
            }

            DrawEntry(entry);
        }

        if (isAtBottom || !_hasUnseenEntries)
        {
            ImGui.SetScrollHereY(1.0f);
            _hasUnseenEntries = false;
        }
    }

    private void DrawEntry(CombatLogEntry entry)
    {
        string timestamp = entry.Timestamp.ToString("HH:mm:ss");
        string phrase = entry.Phrase;

        // Downed events get a full-width highlighted line instead of the normal layout
        if (entry.Phase is "MonsterDefeated" or "PlayerDowned")
        {
            Vector4 downedColor = entry.Phase == "MonsterDefeated"
                ? new Vector4(0.4f, 0.9f, 0.5f, 1f)   // green — monster down is good
                : new Vector4(1f, 0.5f, 0.3f, 1f);     // orange — player down is bad

            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f)))
                ImGui.TextUnformatted(timestamp);

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Text, downedColor))
                ImGui.TextUnformatted($"▶  {phrase}");

            return;
        }

        Vector4 phraseColor = entry.Success
            ? new Vector4(0.85f, 0.95f, 0.85f, 1f)
            : new Vector4(0.95f, 0.75f, 0.75f, 1f);

        Vector4 metaColor = new(0.5f, 0.5f, 0.5f, 1f);

        // Timestamp
        using (ImRaii.PushColor(ImGuiCol.Text, metaColor))
            ImGui.TextUnformatted(timestamp);

        ImGui.SameLine();

        // Tactical phrase
        using (ImRaii.PushColor(ImGuiCol.Text, phraseColor))
            ImGui.TextUnformatted(phrase);

        // Roll detail — gated by ShowRollBreakdown config
        if (plugin.Configuration.ShowRollBreakdown)
        {
            string rollInfo = $"[{entry.Roll} vs DC {entry.DC}]";
            Vector4 rollColor = entry.Success
                ? new Vector4(0.4f, 0.9f, 0.4f, 1f)
                : new Vector4(0.9f, 0.4f, 0.4f, 1f);

            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, rollColor))
                ImGui.TextUnformatted(rollInfo);
        }
    }
}
