using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Services;
namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AliasesAttribute : AliasAttribute
    {
        public AliasesAttribute([CallerMemberName] string memberName = "")
            : base(CommandNameLoadHelper.GetAliasesFor(memberName))
        {
        }
    }
}
