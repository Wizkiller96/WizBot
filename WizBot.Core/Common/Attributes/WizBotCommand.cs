using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WizBotCommandAttribute : CommandAttribute
    {
        public WizBotCommandAttribute([CallerMemberName] string memberName = "") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ')[0])
        {

        }
    }
}