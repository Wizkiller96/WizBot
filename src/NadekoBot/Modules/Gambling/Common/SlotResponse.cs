using System.Collections.Generic;

namespace NadekoBot.Modules.Gambling
{
    public class SlotResponse
    {
        public float Multiplier { get; set; }
        public long Won { get; set; }
        public List<int> Rolls { get; set; } = new List<int>();
        public GamblingError Error { get; set; }
    }
}