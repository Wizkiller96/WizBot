﻿using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using WizBot.Core.Services.Impl;
using Newtonsoft.Json;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UsageAttribute : RemarksAttribute
    {
        // public static string GetUsage(string memberName)
        // {
        //     var usage = Localization.LoadCommand(memberName.ToLowerInvariant()).Usage;
        //     return JsonConvert.SerializeObject(usage);
        // }
        public UsageAttribute(string text = "") : base(text)
        {
        }
    }
}
