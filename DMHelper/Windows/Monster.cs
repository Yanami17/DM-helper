using System;
using System.Collections.Generic;
using System.Text;

namespace DMHelper.Windows;

public class Monster
{
    public string Name { get; set; } = string.Empty;
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int DC { get; set; }
    public bool IsEngaged { get; set; } = false;
}
