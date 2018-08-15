using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DescriptionAttribute : SummaryAttribute
    {
        public DescriptionAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Desc)
        {

        }
    }
}
