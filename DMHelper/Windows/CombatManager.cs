using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using DMHelper.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace DMHelper.Windows;

public class CombatManager
{
    private readonly Plugin plugin;

    public CombatPhase CurrentPhase { get; private set; } = CombatPhase.None;

    public readonly Dictionary<uint, Dictionary<Guid, int>> AttackRolls = new();
    public readonly Dictionary<uint, int> DefenseRolls = new();
    public readonly Dictionary<uint, PlayerState> PlayerStates = new();
    public readonly Dictionary<uint, List<Guid>> DeclaredTargets = new();
    public readonly Dictionary<Guid, List<uint>> MonsterTargets = new();

    public readonly List<CombatLogEntry> CombatLog = new();

    public int PlayerBaseDamage => plugin.Configuration.DefaultPlayerDamage;
    public int MonsterBaseDamage => plugin.Configuration.DefaultMonsterDamage;

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
        if (CurrentPhase == CombatPhase.None)
            return;

        bool isParty = type == XivChatType.Party;
        bool isSay = type == XivChatType.Say;

        if (!isParty && !isSay)
            return;

        // Party chat format:  "Random! ... 7"         (number at end)
        // Say chat format:    "Random! You roll a 7 (out of 20)."
        var matchParty = Regex.Match(message.TextValue, @"Random!.*?(\d+)$");
        var matchSay = Regex.Match(message.TextValue, @"Random! You roll a (\d+) \(out of \d+\)");

        if (plugin.Configuration.DebugMode)
        {
            Plugin.Log.Debug($"[DMHelper] Chat captured | Type: {type} | Sender: '{sender.TextValue}' | Message: '{message.TextValue}'");
            Plugin.Log.Debug($"[DMHelper] Regex results | matchParty: {matchParty.Success} | matchSay: {matchSay.Success}");
        }

        int roll;

        if (isSay && matchSay.Success)
        {
            roll = int.Parse(matchSay.Groups[1].Value);
            Plugin.Log.Information($"[DMHelper] Say roll parsed: {roll}");
        }
        else if (isParty && matchParty.Success)
        {
            roll = int.Parse(matchParty.Groups[1].Value);
            Plugin.Log.Information($"[DMHelper] Party roll parsed: {roll}");
        }
        else
        {
            if (plugin.Configuration.DebugMode)
                Plugin.Log.Debug($"[DMHelper] No roll pattern matched — message ignored.");
            return;
        }

        // --- SAY CHAT: route to the fake member with ListenOnSay active ---
        if (isSay)
        {
            var fakeMember = plugin.FakePartyMembers.FirstOrDefault(f => f.ListenOnSay);
            if (fakeMember == null)
            {
                Plugin.Log.Warning("Say roll ignored: no fake member has ListenOnSay enabled.");
                return;
            }

            Plugin.Log.Information($"Say roll {roll} routed to fake member '{fakeMember.Name}'");
            StoreRoll(fakeMember.EntityId, roll);
            return;
        }

        // --- PARTY CHAT: normal real-member matching ---
        var partyList = Plugin.PartyList;
        if (partyList == null)
            return;

        var rawSender = sender.TextValue.Trim();
        rawSender = Regex.Replace(rawSender, @"^[^\p{L}]+", "");

        var member = partyList.FirstOrDefault(m =>
        {
            if (m == null) return false;
            var baseName = m.Name.TextValue.Split('@')[0].Trim();
            return rawSender.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        });

        if (member == null)
        {
            Plugin.Log.Warning($"Roll ignored: Sender '{rawSender}' not found in party list.");
            return;
        }

        Plugin.Log.Information($"Party roll {roll} stored for {member.Name.TextValue}");
        StoreRoll(member.EntityId, roll);
    }

    // =========================================================
    // ROLL STORAGE (shared between real and fake members)
    // =========================================================

    public void StoreRoll(uint entityId, int roll)
    {
        if (CurrentPhase == CombatPhase.Attack)
        {
            if (!AttackRolls.ContainsKey(entityId))
                AttackRolls[entityId] = new Dictionary<Guid, int>();

            // Store the roll against a placeholder — targets are assigned by the DM after rolling
            // ResolveAttackPhase will map rolls to declared targets at resolution time
            AttackRolls[entityId][Guid.Empty] = roll;
        }
        else if (CurrentPhase == CombatPhase.Defense)
        {
            DefenseRolls[entityId] = roll;
        }
    }

    // =========================================================
    // MANUAL ROLL (for fake members without ListenOnSay)
    // =========================================================

    public void RollForFakeMember(FakePartyMember fakeMember)
    {
        int roll = new Random().Next(1, 21);
        LogManualRoll(fakeMember, roll);
    }

    // =========================================================
    // NAME LOOKUP (real + fake)
    // =========================================================

    private string GetPlayerName(uint entityId)
    {
        var partyList = Plugin.PartyList;
        if (partyList != null)
        {
            foreach (var member in partyList)
            {
                if (member != null && member.EntityId == entityId)
                    return member.Name.TextValue;
            }
        }

        var fake = plugin.FakePartyMembers.FirstOrDefault(f => f.EntityId == entityId);
        if (fake != null)
            return fake.Name;

        return "Unknown";
    }

    public bool HasRolled(uint entityId)
    {
        if (CurrentPhase == CombatPhase.Attack)
            return AttackRolls.ContainsKey(entityId);
        if (CurrentPhase == CombatPhase.Defense)
            return DefenseRolls.ContainsKey(entityId);
        return false;
    }

    public bool HasPendingRolls =>
        AttackRolls.Count > 0 || DefenseRolls.Count > 0;

    public void OnMonsterRemoved(Monster monster)
    {
        // Clean up any player declared targets pointing at this monster
        foreach (var list in DeclaredTargets.Values)
            list.Remove(monster.Id);

        // Clean up monster's own target list
        MonsterTargets.Remove(monster.Id);

        Plugin.Log.Information($"[DMHelper] Cleaned up targets for removed monster '{monster.Name}'");
    }

    public void LogManualRoll(FakePartyMember fake, int roll)
    {
        Plugin.Log.Information($"[DMHelper] Manual roll {roll} generated for fake member '{fake.Name}'");
        StoreRoll(fake.EntityId, roll);
    }

    public void DeclareTarget(uint playerId, Guid monsterId)
    {
        if (!DeclaredTargets.ContainsKey(playerId))
            DeclaredTargets[playerId] = new List<Guid>();

        if (!DeclaredTargets[playerId].Contains(monsterId))
            DeclaredTargets[playerId].Add(monsterId);
    }

    public void DeclareMonsterTarget(Guid monsterId, uint playerId)
    {
        if (!MonsterTargets.ContainsKey(monsterId))
            MonsterTargets[monsterId] = new List<uint>();

        if (!MonsterTargets[monsterId].Contains(playerId))
            MonsterTargets[monsterId].Add(playerId);
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
            Phase = "Attack",
            Phrase = CombatPhrases.Get("Attack", success, playerName, monster.Name, damage)
        });
    }

    private void ResolveAttackPhase()
    {
        foreach (var playerEntry in AttackRolls)
        {
            uint playerId = playerEntry.Key;

            if (!PlayerStates.TryGetValue(playerId, out var player))
                continue;

            // Get the roll value — stored under Guid.Empty placeholder
            if (!playerEntry.Value.TryGetValue(Guid.Empty, out int roll))
                continue;

            // Require explicit targets declared by the DM
            if (!DeclaredTargets.TryGetValue(playerId, out var targets) || targets.Count == 0)
            {
                Plugin.Log.Warning($"[DMHelper] {GetPlayerName(playerId)} rolled but has no targets declared — skipping.");
                continue;
            }

            foreach (var monsterId in targets)
            {
                var monster = plugin.Monsters.FirstOrDefault(m => m.Id == monsterId);
                if (monster == null || monster.CurrentHP <= 0)
                    continue;

                ResolveAttack(playerId, player, monster, roll);
            }

            // If every declared target was already defeated, log it
            bool anyResolved = targets.Any(id =>
            {
                var m = plugin.Monsters.FirstOrDefault(x => x.Id == id);
                return m != null && m.CurrentHP > 0;
            });

            if (!anyResolved)
            {
                string playerName = GetPlayerName(playerId);
                CombatLog.Add(new CombatLogEntry
                {
                    Actor = playerName,
                    Target = string.Empty,
                    Phase = "Attack",
                    Phrase = $"{playerName}'s targets were already defeated — nothing to resolve.",
                });
            }
        }

        // Apply or stage pending damage based on AutoApplyDamage config
        if (plugin.Configuration.AutoApplyDamage)
            ApplyPendingDamage();
        // else: pending damage sits on monsters until DM confirms via MainWindow
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

            if (plugin.Configuration.ClampHPToZero && player.CurrentHP < 0)
                player.CurrentHP = 0;

            if (player.CurrentHP <= 0)
            {
                player.CurrentHP = 0;
                Plugin.Log.Information($"[DMHelper] {playerName} has been downed.");

                CombatLog.Add(new CombatLogEntry
                {
                    Actor = monster.Name,
                    Target = playerName,
                    Roll = 0,
                    DC = 0,
                    Success = true,
                    Damage = 0,
                    Phase = "PlayerDowned",
                    Phrase = CombatPhrases.Get("PlayerDowned", true, monster.Name, playerName, 0)
                });
            }
        }

        CombatLog.Add(new CombatLogEntry
        {
            Actor = monster.Name,
            Target = playerName,
            Roll = roll,
            DC = monster.DC,
            Success = success,
            Damage = damage,
            Phase = "Defense",
            Phrase = CombatPhrases.Get("Defense", success, monster.Name, playerName, damage)
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

            // Only monsters that have explicitly declared this player as a target
            var attackers = plugin.Monsters
                .Where(m => m.IsEngaged && m.CurrentHP > 0
                    && MonsterTargets.TryGetValue(m.Id, out var targets)
                    && targets.Contains(playerId))
                .ToList();

            if (attackers.Count == 0)
            {
                Plugin.Log.Warning($"[DMHelper] Defense roll ignored for entity {playerId} — no monster has declared them as a target.");
                continue;
            }

            foreach (var monster in attackers)
                ResolveDefense(playerId, player, monster, roll);
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

        if (plugin.Configuration.ClearRollsAfterResolve)
            ClearRolls();

        ClearDeclaredTargets();
    }

    // Called by MainWindow when AutoApplyDamage is off and DM confirms damage
    public void ApplyPendingDamage()
    {
        foreach (var monster in plugin.Monsters)
        {
            if (monster.PendingDamage <= 0)
                continue;

            Plugin.Log.Information($"[DMHelper] Applying {monster.PendingDamage} damage to {monster.Name}");
            monster.CurrentHP = Math.Max(0, monster.CurrentHP - monster.PendingDamage);
            monster.PendingDamage = 0;

            if (monster.CurrentHP == 0)
            {
                monster.IsEngaged = false;
                CombatLog.Add(new CombatLogEntry
                {
                    Actor = monster.Name,
                    Target = monster.Name,
                    Phase = "MonsterDefeated",
                    Phrase = CombatPhrases.Get("MonsterDefeated", true, string.Empty, monster.Name, 0)
                });
            }
        }
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
        MonsterTargets.Clear();
    }

    // =========================================================
    // ENUMS & NESTED TYPES
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
