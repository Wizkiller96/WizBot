using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services;
using WizBot.Services.Impl;

namespace WizBot.Common.Attributes
{
    public class WizBotCommand : CommandAttribute
    {
        public WizBotCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ')[0])
        {

        }
    }
}
