﻿using CommandLine;
using WizBot.Core.Common;

namespace WizBot.Core.Modules.Gambling.Common.AnimalRacing
{
    public class RaceOptions : IWizBotCommandOptions
    {
        [Option('s', "start-time", Default = 20, Required = false)]
        public int StartTime { get; set; } = 20;

        public void NormalizeOptions()
        {
            if (this.StartTime < 10 || this.StartTime > 120)
                this.StartTime = 20;
        }
    }
}