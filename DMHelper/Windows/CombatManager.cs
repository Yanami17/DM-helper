using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using DMHelper.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;

namespace DMHelper.Windows;

public class CombatManager
{
    private readonly Plugin plugin;

    public CombatPhase CurrentPhase { get; private set; } = CombatPhase.None;

    public readonly Dictionary<uint, Dictionary<Guid, int>> AttackRolls = new();
    public readonly Dictionary<uint, int> DefenseRolls = new();
    public readonly Dictionary<uint, PlayerState> PlayerStates = new();
    public readonly Dictionary<uint, List<Guid>> DeclaredTargets = new();

    public readonly List<CombatLogEntry> CombatLog = new();

    public int PlayerBaseDamage = 1;
    public int MonsterBaseDamage = 1;

    public CombatManager(Plugin plugin)
    {
        Plugin.Log.Information("CombatManager constructed");
        this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        Plugin.ChatGui!.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        Plugin.ChatGui!.ChatMessage -= OnChatMessage;
    }

    // =========================================================
    // CHAT LISTENER
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

        Plugin.Log.Information("---- Sender Payload Dump ----");

        foreach (var p in sender.Payloads)
        {
            Plugin.Log.Information($"Type: {p.GetType().Name} | Text: '{p.ToString()}'");
        }

        Plugin.Log.Information($"Flattened TextValue: '{sender.TextValue}'");
        Plugin.Log.Information("--------------------------------");


        // --- NORMALIZED NAME MATCHING (Cross-World Safe) ---

        // Get sender text as displayed
        var rawSender = sender.TextValue.Trim();

        // Remove leading formatting junk
        rawSender = Regex.Replace(rawSender, @"^[^\p{L}]+", "");

        var member = partyList.FirstOrDefault(m =>
        {
            if (m == null)
                return false;

            var partyNameFull = m.Name.TextValue;
            var partyBaseName = partyNameFull.Split('@')[0].Trim();

            // If sender begins with the character name, it's a match
            return rawSender.StartsWith(partyBaseName, StringComparison.OrdinalIgnoreCase);
        });

        if (member == null)
        {
            Plugin.Log.Warning($"Roll ignored: Sender '{rawSender}' not found in party list.");
            return;
        }

        uint playerId = member.EntityId;


        if (CurrentPhase == CombatPhase.Attack)
        {
            if (!DeclaredTargets.TryGetValue(playerId, out var targets))
            {
                Plugin.Log.Warning("Roll ignored: No target declared.");
                targets = plugin.Monsters
                    .Where(m => m.IsEngaged && m.CurrentHP > 0)
                    .Select(m => m.Id)
                    .ToList();

                if (targets.Count == 0)
                    return;
            }

            if (!AttackRolls.ContainsKey(playerId))
                AttackRolls[playerId] = new Dictionary<Guid, int>();

            foreach (var monsterId in targets)
                AttackRolls[playerId][monsterId] = roll;
        }
        else if (CurrentPhase == CombatPhase.Defense)
        {
            DefenseRolls[playerId] = roll;
        }

        Plugin.Log.Information($"Stored roll {roll} for {member.Name.TextValue}");
        Plugin.Log.Information($"AttackRolls players: {AttackRolls.Count}");
        Plugin.Log.Information($"DefenseRolls players: {DefenseRolls.Count}");
    }

    private string GetPlayerName(uint playerId)
    {
        var partyList = Plugin.PartyList;
        if (partyList == null)
            return "Unknown";

        foreach (var member in partyList)
        {
            if (member == null)
                continue;

            if (member.EntityId == playerId)
                return member.Name.TextValue;
        }

        return "Unknown";
    }

    public void DeclareTarget(uint playerId, Guid monsterId)
    {
        if (!DeclaredTargets.ContainsKey(playerId))
            DeclaredTargets[playerId] = new List<Guid>();

        if (!DeclaredTargets[playerId].Contains(monsterId))
            DeclaredTargets[playerId].Add(monsterId);
    }

    private void ResolveAttack(uint playerId, PlayerState player, Monster monster, int roll)
    {
        bool success = roll >= monster.DC;
        int damage = success ? PlayerBaseDamage : 0;

        string playerName = GetPlayerName(playerId);

        if (success)
        {
            player.Status = "Hit";
            monster.PendingDamage += damage;
        }
        else
        {
            player.Status = "Miss";
        }

        CombatLog.Add(new CombatLogEntry
        {
            Actor = playerName,
            Target = monster.Name,
            Roll = roll,
            DC = monster.DC,
            Success = success,
            Damage = damage,
            Phase = "Attack"
        });
    }

    private void ResolveAttackPhase()
    {
        foreach (var playerEntry in AttackRolls)
        {
            uint playerId = playerEntry.Key;

            if (!PlayerStates.TryGetValue(playerId, out var player))
                continue;

            foreach (var targetEntry in playerEntry.Value)
            {
                Guid monsterId = targetEntry.Key;
                int roll = targetEntry.Value;

                var monster = plugin.Monsters.FirstOrDefault(m => m.Id == monsterId);
                if (monster == null || monster.CurrentHP <= 0)
                    continue;

                ResolveAttack(playerId, player, monster, roll);
            }
        }
    }

    private void ResolveDefense(uint playerId, PlayerState player, Monster monster, int roll)
    {
        bool success = roll >= monster.DC;
        int damage = success ? 0 : MonsterBaseDamage;

        string playerName = GetPlayerName(playerId);

        if (success)
        {
            player.Status = "Defended";
        }
        else
        {
            player.Status = "Failed";
            player.CurrentHP -= damage;
        }

        CombatLog.Add(new CombatLogEntry
        {
            Actor = monster.Name,
            Target = playerName,
            Roll = roll,
            DC = monster.DC,
            Success = success,
            Damage = damage,
            Phase = "Defense"
        });
    }

    private void ResolveDefensePhase()
    {
        foreach (var kvp in DefenseRolls)
        {
            uint playerId = kvp.Key;
            int roll = kvp.Value;

            if (!PlayerStates.TryGetValue(playerId, out var player))
                continue;

            // All engaged monsters attack
            var attackers = plugin.Monsters
                .Where(m => m.IsEngaged && m.CurrentHP > 0)
                .ToList();

            foreach (var monster in attackers)
            {
                ResolveDefense(playerId, player, monster, roll);
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
        if (CurrentPhase == CombatPhase.Attack)
            ResolveAttackPhase();
        else if (CurrentPhase == CombatPhase.Defense)
            ResolveDefensePhase();

        ClearRolls();
        ClearDeclaredTargets();
    }

    public void ClearRolls()
    {
        AttackRolls.Clear();
        DefenseRolls.Clear();
    }


    public void ClearStatuses()
    {
        foreach (var state in PlayerStates.Values)
            state.Status = "";
    }
    public void ClearDeclaredTargets()
    {
        DeclaredTargets.Clear();
    }


    // =========================================================
    // ADDITIONAL CONSTRUCTORS
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
