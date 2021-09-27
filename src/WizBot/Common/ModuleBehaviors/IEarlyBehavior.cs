﻿using System.Threading.Tasks;
using Discord;

namespace WizBot.Common.ModuleBehaviors
{
    /// <summary>
    /// Implemented by modules which block execution before anything is executed
    /// </summary>
    public interface IEarlyBehavior
    {
        int Priority { get; }
        Task<bool> RunBehavior(IGuild guild, IUserMessage msg);
    }
}
