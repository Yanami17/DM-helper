using System;

namespace DMHelper.Windows;

public class FakePartyMember
{
    public Guid Id { get; } = Guid.NewGuid();

    // Use a high arbitrary uint range to avoid colliding with real entity IDs
    public uint EntityId { get; } = (uint)(0xF0000000 + new Random().Next(0x0FFFFFFF));

    public string Name { get; set; } = "Fake Member";

    public bool ListenOnSay { get; set; } = false;
}
