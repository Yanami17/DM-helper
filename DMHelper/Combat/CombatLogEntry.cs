using System;

namespace DMHelper.Combat;

public class CombatLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Actor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;

    public int Roll { get; set; }
    public int DC { get; set; }

    public bool Success { get; set; }
    public int Damage { get; set; }

    public string Phase { get; set; } = string.Empty;
}
