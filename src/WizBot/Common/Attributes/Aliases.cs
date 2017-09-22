using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services.Impl;

//todo what if it doesn't exist

namespace WizBot.Common.Attributes
{
    public class Aliases : AliasAttribute
    {
        public Aliases([CallerMemberName] string memberName = "") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ').Skip(1).ToArray())
        {
        }
    }
}