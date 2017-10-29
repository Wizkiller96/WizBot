using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;
using System.Linq;
using Discord;

namespace WizBot.Common.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Usage.GetUsage(memberName))
        {

        }

        public static string GetUsage(string memberName)
        {
            var usage = Localization.LoadCommand(memberName.ToLowerInvariant()).Usage;
            return string.Join(" or ", usage
                .Select(x => Format.Code(x)));
        }
    }
}