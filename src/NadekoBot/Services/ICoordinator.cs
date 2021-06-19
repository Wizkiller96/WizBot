using System;
using System.Collections.Generic;

namespace NadekoBot.Services
{
    public interface ICoordinator
    {
        bool RestartBot();
        void Die();
        bool RestartShard(int shardId);
        IEnumerable<ShardStatus> GetAllShardStatuses();
        int GetGuildCount();
    }
    
    public class ShardStatus
    {
        public Discord.ConnectionState ConnectionState { get; set; }
        public DateTime Time { get; set; }
        public int ShardId { get; set; }
        public int Guilds { get; set; }
    }
}