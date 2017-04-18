using Discord.Commands;
using WizBot.Services;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WizBot.Attributes
{
    public class Aliases : AliasAttribute
    {
        public Aliases([CallerMemberName] string memberName = "") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ').Skip(1).ToArray())
        {
        }
    }
}
