﻿using System;
using System.Security.Cryptography;

namespace WizBot.Services
{
    public class WizBotRandom : Random
    {
        RandomNumberGenerator rng;

        public WizBotRandom() : base()
        {
            rng = RandomNumberGenerator.Create();
        }

        private WizBotRandom(int Seed) : base(Seed)
        {
            rng = RandomNumberGenerator.Create();
        }

        public override int Next()
        {
            var bytes = new byte[sizeof(int)];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0));
        }

        public override int Next(int maxValue)
        {
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException();
            var bytes = new byte[sizeof(int)];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0)) % maxValue;
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException();
            if (minValue == maxValue)
                return minValue;
            var bytes = new byte[sizeof(int)];
            rng.GetBytes(bytes);
            var num = BitConverter.ToInt32(bytes, 0);
            var sign = Math.Sign(BitConverter.ToInt32(bytes, 0));
            return (sign * BitConverter.ToInt32(bytes, 0)) % (maxValue - minValue) + minValue;
        }

        public override void NextBytes(byte[] buffer)
        {
            rng.GetBytes(buffer);
        }

        protected override double Sample()
        {
            var bytes = new byte[sizeof(double)];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToDouble(bytes, 0) / double.MaxValue + 1);
        }

        public override double NextDouble()
        {
            var bytes = new byte[sizeof(double)];
            rng.GetBytes(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }
    }
}
