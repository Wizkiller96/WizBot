using Discord.Commands;
using System;

namespace WizBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class WizBotModuleAttribute : GroupAttribute
    {
        public WizBotModuleAttribute(string moduleName) : base("")
        {
        }
    }
}
