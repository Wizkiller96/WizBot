using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services.Impl;

namespace WizBot.Common.Attributes
{
    public class Aliases : AliasAttribute
    {
        public Aliases([CallerMemberName] string memberName = "") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ').Skip(1).ToArray())
        {
        }
    }
}
