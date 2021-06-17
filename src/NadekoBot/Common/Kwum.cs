using System;
using System.Runtime.CompilerServices;

namespace NadekoBot.Core.Common
{
    // needs proper invalid input check (character array input out of range)
    // needs negative number support
    public readonly struct kwum : IEquatable<kwum>
    {
        private readonly int _value;
        private const string ValidCharacters = "23456789abcdefghijkmnpqrstuvwxyz";

        public kwum(int num)
            => _value = num;
        
        public kwum(in char c)
        {
            if (!IsValidChar(c))
                throw new ArgumentException("Character needs to be a valid kwum character.", nameof(c));

            _value = InternalCharToValue(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InternalCharToValue(in char c) 
            => ValidCharacters.IndexOf(c);

        public kwum(in ReadOnlySpan<char> input)
        {;
            _value = 0;
            for (var index = 0; index < input.Length; index++)
            {
                var c = input[index];
                if (!IsValidChar(c))
                    throw new ArgumentException("All characters need to be a valid kwum characters.", nameof(input));

                _value += ValidCharacters.IndexOf(c) * (int)Math.Pow(ValidCharacters.Length, input.Length - index - 1);
            }
        }

        public static bool TryParse(in ReadOnlySpan<char> input, out kwum value)
        {
            value = default;
            foreach(var c in input)
                if (!IsValidChar(c))
                    return false;

            value = new kwum(input);
            return true;
        }

        public static kwum operator +(kwum left, kwum right)
            => new kwum(left._value + right._value);

        public static bool operator ==(kwum left, kwum right)
            => left._value == right._value;

        public static bool operator !=(kwum left, kwum right)
            => !(left == right);

        public static implicit operator long(kwum kwum)
            => kwum._value;
        
        public static implicit operator int(kwum kwum)
            => kwum._value;
        public static implicit operator kwum(int num)
            => new kwum(num);

        public static bool IsValidChar(char c)
            => ValidCharacters.Contains(c);

        public override string ToString()
        {
            var count = ValidCharacters.Length;
            var localValue = _value;
            var arrSize = (int)Math.Log(localValue, count) + 1;
            Span<char> chars = new char[arrSize];
            while (localValue > 0)
            {
                localValue = Math.DivRem(localValue, count, out var rem);
                chars[--arrSize] = ValidCharacters[(int)rem];
            }

            return new string(chars);
        }

        public override bool Equals(object obj)
            => obj is kwum kw && kw == this;

        public bool Equals(kwum other)
            => other == this;

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}