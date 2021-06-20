using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Services;
namespace NadekoBot.Common.Attributes
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
