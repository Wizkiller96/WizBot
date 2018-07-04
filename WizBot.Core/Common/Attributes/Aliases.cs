using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;
namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AliasesAttribute : AliasAttribute
    {
        public AliasesAttribute([CallerMemberName] string memberName = "") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ').Skip(1).ToArray())
        {
        }
    }
}