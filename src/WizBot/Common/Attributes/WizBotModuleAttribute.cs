using System;
using Discord.Commands;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class WizBotModuleAttribute : GroupAttribute
    {
        public WizBotModuleAttribute(string moduleName) : base(moduleName)
        {
        }
    }
}

