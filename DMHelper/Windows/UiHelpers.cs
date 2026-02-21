using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace DMHelper.Windows
{
    public static class UiHelpers
    {
        public static void DrawHpBar(int current, int max, Vector2 size)
        {
            max = Math.Max(1, max);
            current = Math.Clamp(current, 0, max);

            float percent = (float)current / max;

            Vector4 color =
                percent > 0.6f ? new Vector4(0.2f, 0.8f, 0.2f, 1f) :
                percent > 0.3f ? new Vector4(0.9f, 0.7f, 0.2f, 1f) :
                                 new Vector4(0.9f, 0.2f, 0.2f, 1f);

            using (ImRaii.PushColor(ImGuiCol.PlotHistogram, color))
            {
                ImGui.ProgressBar(percent, size, $"{current}/{max}");
            }
        }
        public static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "?";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
        }
    }

}
