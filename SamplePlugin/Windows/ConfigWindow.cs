using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{

    private string newMonsterName = string.Empty;
    private int newMonsterHP = 10;
    private int newMonsterDC = 10;

    private readonly Plugin plugin;
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin)
        : base("DM Configs")
    {
        this.plugin = plugin;
        this.configuration = plugin.Configuration;

        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 250); // Slightly bigger now
        SizeCondition = ImGuiCond.Always;
    }


    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        // Can't ref a property, so use a local copy
        var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            configuration.SomePropertyToBeSavedAndWithADefault = configValue;
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }

        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }

        ImGui.Separator();
        ImGui.Text("Add Monster");

        ImGui.InputText("Name", ref newMonsterName, 100);
        ImGui.InputInt("HP", ref newMonsterHP);
        ImGui.InputInt("DC", ref newMonsterDC);

        if (ImGui.Button("Add Monster"))
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

                newMonsterName = string.Empty;
                newMonsterHP = 10;
                newMonsterDC = 10;
            }
        }
    }
}
