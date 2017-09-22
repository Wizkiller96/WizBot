using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services.Impl;

namespace WizBot.Common.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Desc)
        {

        }
    }
}