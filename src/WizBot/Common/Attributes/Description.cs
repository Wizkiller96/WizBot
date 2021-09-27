using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DescriptionAttribute : SummaryAttribute
    {
        // Localization.LoadCommand(memberName.ToLowerInvariant()).Desc
        public DescriptionAttribute(string text = "") : base(text)
        {
        }
    }
}
