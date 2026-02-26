using System;
using System.Collections.Generic;

namespace DMHelper.Windows;

public static class CombatPhrases
{
    private static readonly Random Rng = new();

    // ── Monster Defeated ─────────────────────────────────────
    private static readonly string[] MonsterDefeated =
    [
        "{target} collapses. Out of the fight.",
        "{target} is down. Threat eliminated.",
        "Defeated. {target} is no longer a factor.",
        "{target} falls. Combat continues.",
        "{target} can't go on. Taken out.",
        "The last blow lands. {target} is defeated.",
        "{target} drops. One less to worry about.",
    ];

    // ── Player Downed ────────────────────────────────────────
    private static readonly string[] PlayerDowned =
    [
        "{target} is down. Out of the fight.",
        "{target} hits the ground. Incapacitated.",
        "Downed. {target} can no longer act.",
        "{target} falls. Someone needs to step up.",
        "{target} is out. The party is short a member.",
        "Critical hit home. {target} is incapacitated.",
        "{target} can't continue. They're down.",
    ];

    // ── Attack: Hit ──────────────────────────────────────────
    private static readonly string[] AttackHit =
    [
        "{actor} connects. {target} takes {damage}.",
        "{actor} lands the blow on {target}. {damage} damage.",
        "Strike confirmed. {target} is hit for {damage}.",
        "{actor} breaks through. {target} suffers {damage}.",
        "{actor} finds the mark. {damage} to {target}.",
        "Clean hit. {target} takes {damage} from {actor}.",
        "{actor} drives through {target}'s guard. {damage} damage dealt.",
    ];

    // ── Attack: Miss ─────────────────────────────────────────
    private static readonly string[] AttackMiss =
    [
        "{actor} swings wide. {target} holds.",
        "No effect. {actor} fails to connect with {target}.",
        "{target} turns aside {actor}'s attack.",
        "{actor} misses. {target} remains unscathed.",
        "Attack falls short. {target} stands firm.",
        "{actor}'s strike goes wide of {target}.",
        "Glancing blow. {target} shrugs it off.",
    ];

    // ── Defense: Defended ────────────────────────────────────
    private static readonly string[] DefenseSuccess =
    [
        "{target} weathers the assault from {actor}.",
        "{target} holds position. {actor}'s attack fails.",
        "Defended. {actor} cannot break {target}.",
        "{target} braces and survives {actor}'s strike.",
        "{actor} presses, but {target} doesn't yield.",
        "{target} reads {actor}'s move and deflects.",
        "No damage. {target} withstands {actor}.",
    ];

    // ── Defense: Failed ──────────────────────────────────────
    private static readonly string[] DefenseFail =
    [
        "{actor} punishes {target}. {damage} damage.",
        "{target} fails to block. {actor} deals {damage}.",
        "Exposed. {target} takes {damage} from {actor}.",
        "{actor} capitalises on the opening. {damage} to {target}.",
        "{target} takes {damage}. Defense broken by {actor}.",
        "{actor} gets through. {damage} damage to {target}.",
        "{target} couldn't hold. {damage} damage dealt by {actor}.",
    ];

    // ── Tracks last used index per category to avoid repeats ─
    private static readonly Dictionary<string, int> LastIndex = new();

    public static string Get(string phase, bool success, string actor, string target, int damage)
    {
        string key = $"{phase}_{success}";

        string[] pool = (phase, success) switch
        {
            ("Attack", true) => AttackHit,
            ("Attack", false) => AttackMiss,
            ("Defense", true) => DefenseSuccess,
            ("Defense", false) => DefenseFail,
            ("MonsterDefeated", _) => MonsterDefeated,
            ("PlayerDowned", _) => PlayerDowned,
            _ => AttackMiss
        };

        LastIndex.TryGetValue(key, out int last);

        int index;
        do { index = Rng.Next(pool.Length); }
        while (pool.Length > 1 && index == last);

        LastIndex[key] = index;

        return pool[index]
            .Replace("{actor}", actor)
            .Replace("{target}", target)
            .Replace("{damage}", damage.ToString());
    }
}
