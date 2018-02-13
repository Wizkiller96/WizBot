using System.Collections.Immutable;
using WizBot.Common;

namespace WizBot.Modules.Gambling.Common.WheelOfFortune
{
    public class WheelOfFortune
    {
        private static readonly WizBotRandom _rng = new WizBotRandom();

        private static readonly ImmutableArray<string> _emojis = new string[] {
            "⬆",
            "↖",
            "⬅",
            "↙",
            "⬇",
            "↘",
            "➡",
            "↗" }.ToImmutableArray();

        public static readonly ImmutableArray<float> Multipliers = new float[] {
            1.7f,
            1.5f,
            0.2f,
            0.1f,
            0.3f,
            0.5f,
            1.2f,
            2.4f,
        }.ToImmutableArray();

        public int Result { get; }
        public string Emoji => _emojis[Result];
        public float Multiplier => Multipliers[Result];

        public WheelOfFortune()
        {
            this.Result = _rng.Next(0, 8);
        }
    }
}