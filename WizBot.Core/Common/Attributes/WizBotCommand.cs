using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;

namespace WizBot.Common.Attributes
{
    public class WizBotCommand : CommandAttribute
    {
        public WizBotCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ')[0])
        {

        }
    }
}