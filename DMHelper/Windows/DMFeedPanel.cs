using Dalamud.Bindings.ImGui;
using DMHelper.Combat;
using System.Numerics;

namespace DMHelper.Windows;

public class DMFeedPanel
{
    private readonly CombatManager combat;

    public DMFeedPanel(CombatManager combat)
    {
        this.combat = combat;
    }

    public void Draw()
    {
        ImGui.Text("Combat Feed");

        if (ImGui.Button("Clear Log"))
        {
            combat.CombatLog.Clear();
        }

        ImGui.Separator();

        if (ImGui.BeginChild("CombatLogChild"))
        {
            foreach (var entry in combat.CombatLog)
            {
                DrawEntry(entry);
            }

            ImGui.EndChild();
        }
    }

    private void DrawEntry(CombatLogEntry entry)
    {
        string text =
            entry.Success
                ? $"{entry.Actor} hits {entry.Target} ({entry.Roll} vs {entry.DC}) for {entry.Damage} damage."
                : $"{entry.Actor} fails against {entry.Target} ({entry.Roll} vs {entry.DC}).";

        if (entry.Success)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 1f, 0.3f, 1f));
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f));
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();
        }
    }
}
