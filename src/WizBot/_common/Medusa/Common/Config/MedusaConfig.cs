﻿#nullable enable
using Cloneable;
using WizBot.Common.Yml;

namespace WizBot.Medusa;

[Cloneable]
public sealed partial class MedusaConfig : ICloneable<MedusaConfig>
{
    [Comment("""DO NOT CHANGE""")]
    public int Version { get; set; } = 1;
    
    [Comment("""List of medusae automatically loaded at startup""")]
    public List<string>? Loaded { get; set; }

    public MedusaConfig()
    {
        Loaded = new();
    }
}