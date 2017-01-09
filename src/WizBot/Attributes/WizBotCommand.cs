using Discord.Commands;
using WizBot.Services;
using System.Runtime.CompilerServices;

namespace WizBot.Attributes
{
    public class WizBotCommand : CommandAttribute
    {
        public WizBotCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ')[0])
        {

        }
    }
}
