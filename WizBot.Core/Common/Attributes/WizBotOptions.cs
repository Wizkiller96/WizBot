using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Common.Attributes
{
    public class WizBotOptions : Attribute
    {
        public Type OptionType { get; set; }

        public WizBotOptions(Type t)
        {
            this.OptionType = t;
        }
    }
}