using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services.Impl;

namespace WizBot.Common.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Usage)
        {

        }
    }
}