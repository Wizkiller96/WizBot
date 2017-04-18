using Discord.Commands;
using WizBot.Services;
using System.Runtime.CompilerServices;

namespace WizBot.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_desc"))
        {

        }
    }
}
