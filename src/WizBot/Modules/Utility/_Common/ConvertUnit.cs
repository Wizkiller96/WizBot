#nullable disable
using System.Diagnostics;

namespace WizBot.Modules.Utility.Common;

[DebuggerDisplay("Type: {UnitType} Trigger: {Triggers[0]} Mod: {Modifier}")]
public class ConvertUnit
{
    public string[] Triggers { get; set; }
    public string UnitType { get; set; }
    public decimal Modifier { get; set; }
}