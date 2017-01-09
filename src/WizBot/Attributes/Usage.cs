using Discord.Commands;
using WizBot.Services;
using System.Runtime.CompilerServices;

namespace WizBot.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant()+"_usage"))
        {

        }
    }
}
