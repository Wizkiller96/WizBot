using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using MorseCode.ITask;

namespace NadekoBot.VotesApi.Services
{
    public class FileVotesCache : IVotesCache
    {
        private const string statsFile = "store/stats.json";
        private const string topggFile = "store/topgg.json";
        private const string discordsFile = "store/discords.json";

        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        public FileVotesCache()
        {
            if (!Directory.Exists("store"))
                Directory.CreateDirectory("store");
            
            if(!File.Exists(topggFile))
                File.WriteAllText(topggFile, "[]");
            
            if(!File.Exists(discordsFile))
                File.WriteAllText(discordsFile, "[]");
        }

        public ITask AddNewTopggVote(string userId)
        {
            return AddNewVote(topggFile, userId);
        }
        
        public ITask AddNewDiscordsVote(string userId)
        {
            return AddNewVote(discordsFile, userId);
        }

        private async ITask AddNewVote(string file, string userId)
        {
            await locker.WaitAsync();
            try
            {
                var votes = await GetVotesAsync(file);
                votes.Add(userId);
                await File.WriteAllTextAsync(file , JsonSerializer.Serialize(votes));
            }
            finally
            {
                locker.Release();
            }
        }

        public async ITask<IList<Vote>> GetNewTopGgVotesAsync()
        {
            var votes = await EvictTopggVotes();
            return votes;
        }

        public async ITask<IList<Vote>> GetNewDiscordsVotesAsync()
        {
            var votes = await EvictDiscordsVotes();
            return votes;
        }

        private ITask<List<Vote>> EvictTopggVotes()
            => EvictVotes(topggFile);

        private ITask<List<Vote>> EvictDiscordsVotes()
            => EvictVotes(discordsFile);

        private async ITask<List<Vote>> EvictVotes(string file)
        {
            await locker.WaitAsync();
            try
            {

                var ids = await GetVotesAsync(file);
                await File.WriteAllTextAsync(file, "[]");
                
                return ids?
                    .Select(x => (Ok: ulong.TryParse(x, out var r), Id: r))
                    .Where(x => x.Ok)
                    .Select(x => new Vote
                    {
                        UserId = x.Id
                    })
                    .ToList();
            }
            finally
            {
                locker.Release();
            }
        }

        private async ITask<IList<string>> GetVotesAsync(string file)
        {
            await using var fs = File.Open(file, FileMode.Open);
            var votes = await JsonSerializer.DeserializeAsync<List<string>>(fs);
            return votes;
        }
    }
}