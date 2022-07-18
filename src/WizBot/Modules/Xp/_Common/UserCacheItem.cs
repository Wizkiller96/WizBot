#nullable disable
using Cloneable;

namespace WizBot.Modules.Xp.Services;

[Cloneable]
public sealed partial class UserXpGainData : ICloneable<UserXpGainData>
{
    public IGuildUser User { get; set; }
    public IGuild Guild { get; set; }
    public IMessageChannel Channel { get; set; }
    public int XpAmount { get; set; }
}