using System;

namespace DMHelper.Windows;

public class Monster
{
    public Guid Id { get; } = Guid.NewGuid();   // ← add this

    public string Name { get; set; } = string.Empty;
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int DC { get; set; }
    public bool IsEngaged { get; set; } = false;

    public int PendingDamage { get; set; } = 0; // ← add this
}
