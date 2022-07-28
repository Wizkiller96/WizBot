namespace Nadeko.Common;

public readonly struct ShmartNumber : IEquatable<ShmartNumber>
{
    public long Value { get; }

    public ShmartNumber(long val)
    {
        Value = val;
    }

    public static implicit operator ShmartNumber(long num)
        => new(num);

    public static implicit operator long(ShmartNumber num)
        => num.Value;

    public static implicit operator ShmartNumber(int num)
        => new(num);

    public override string ToString()
        => Value.ToString();

    public override bool Equals(object? obj)
        => obj is ShmartNumber sn && Equals(sn);

    public bool Equals(ShmartNumber other)
        => other.Value == Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(ShmartNumber left, ShmartNumber right)
        => left.Equals(right);

    public static bool operator !=(ShmartNumber left, ShmartNumber right)
        => !(left == right);
}