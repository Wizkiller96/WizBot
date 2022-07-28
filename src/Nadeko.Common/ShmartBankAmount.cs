namespace Nadeko.Common;

public readonly struct ShmartBankAmount
{
    public long Amount { get; }
    public ShmartBankAmount(long amount)
    {
        Amount = amount;
    }
    
    public static implicit operator ShmartBankAmount(long num)
        => new(num);

    public static implicit operator long(ShmartBankAmount num)
        => num.Amount;

    public static implicit operator ShmartBankAmount(int num)
        => new(num);
}