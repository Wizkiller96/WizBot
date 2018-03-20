using System.Collections.Concurrent;

namespace WizBot.Modules.Administration.Common
{
    public class Ratelimiter
    {
        public ConcurrentDictionary<ulong, uint> Users { get; set; } = new ConcurrentDictionary<ulong, uint>();
        public uint MaxMessages { get; set; }
        public int PerSeconds { get; set; }
    }
}