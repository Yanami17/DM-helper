using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DMHelper.Windows;

public class CombatManager
{
    private readonly Plugin plugin;

    public CombatPhase CurrentPhase { get; private set; } = CombatPhase.None;

    public readonly Dictionary<uint, int> LastRolls = new();
    public readonly Dictionary<uint, PlayerState> PlayerStates = new();

    public int PlayerBaseDamage = 1;
    public int MonsterBaseDamage = 1;

    public CombatManager(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
    }

    // =========================================================
    // CHAT LISTENER (STORE ONLY)
    // =========================================================

    private void OnChatMessage(
        XivChatType type,
        int timestamp,
        ref SeString sender,
        ref SeString message,
        ref bool isHandled)
    {
        if (type != XivChatType.Party)
            return;

        if (CurrentPhase == CombatPhase.None)
            return;

        var match = Regex.Match(message.TextValue, @"Random!.*?(\d+)$");
        if (!match.Success)
            return;

        int roll = int.Parse(match.Groups[1].Value);

        var partyList = Plugin.PartyList;
        if (partyList == null)
            return;

        string cleanName = sender.TextValue.TrimStart('', '', '', '');

        foreach (var member in partyList)
        {
            if (member == null)
                continue;

            if (member.Name.TextValue.Equals(cleanName, StringComparison.Ordinal))
            {
                LastRolls[member.EntityId] = roll;
                Plugin.Log.Information($"Stored roll {roll} for {cleanName}");
                break;
            }
        }
    }

    // =========================================================
    // PHASE CONTROL
    // =========================================================

    public void SetPhase(CombatPhase phase)
    {
        CurrentPhase = phase;
        ClearStatuses();
        ClearRolls();
    }

    public void ResolvePhase()
    {
        if (CurrentPhase == CombatPhase.None)
            return;

        var engagedMonsters = plugin.Monsters
            .Where(m => m.IsEngaged && m.CurrentHP > 0)
            .ToList();

        if (engagedMonsters.Count == 0)
            return;

        foreach (var kvp in LastRolls)
        {
            uint entityId = kvp.Key;
            int roll = kvp.Value;

            if (!PlayerStates.TryGetValue(entityId, out var state))
                continue;

            bool success = engagedMonsters.Any(m => roll >= m.DC);

            if (CurrentPhase == CombatPhase.Attack)
                state.Status = success ? "Hit" : "Miss";
            else if (CurrentPhase == CombatPhase.Defense)
                state.Status = success ? "Defended" : "Failed";
        }
    }

    public void ClearRolls() => LastRolls.Clear();

    public void ClearStatuses()
    {
        foreach (var state in PlayerStates.Values)
            state.Status = "";
    }

    // =========================================================

    public enum CombatPhase
    {
        None,
        Attack,
        Defense
    }

    public class PlayerState
    {
        public int MaxHP = 0;
        public int CurrentHP = 0;
        public string Status = "";
    }
}
