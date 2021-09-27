using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WizBotCommandAttribute : CommandAttribute
    {
        public WizBotCommandAttribute([CallerMemberName] string memberName="") 
            : base(CommandNameLoadHelper.GetCommandNameFor(memberName))
        {
            this.MethodName = memberName.ToLowerInvariant();
        }

        public string MethodName { get; }
    }
}
