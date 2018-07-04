using System;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WizBotOptionsAttribute : Attribute
    {
        public Type OptionType { get; set; }

        public WizBotOptionsAttribute(Type t)
        {
            this.OptionType = t;
        }
    }
}