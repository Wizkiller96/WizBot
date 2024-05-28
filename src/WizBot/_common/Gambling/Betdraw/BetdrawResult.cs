﻿using Wiz.Econ;

namespace WizBot.Modules.Gambling.Betdraw;

public readonly struct BetdrawResult
{
    public decimal Won { get; init; }
    public decimal Multiplier { get; init; }
    public BetdrawResultType ResultType { get; init; }
    public RegularCard Card { get; init; }
}