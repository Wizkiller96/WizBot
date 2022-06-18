﻿namespace WizBot.Modules.Gambling.Bank;

public interface IBankService
{
    Task<bool> DepositAsync(ulong userId, long amount);
    Task<bool> WithdrawAsync(ulong userId, long amount);
    Task<long> GetBalanceAsync(ulong userId);
    Task<long> BurnAllAsync(ulong userId);
}