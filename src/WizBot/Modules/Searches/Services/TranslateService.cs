﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Extensions;
using WizBot.Services;

namespace WizBot.Modules.Searches
{
    public sealed class TranslateService : ITranslateService, ILateExecutor, IReadyExecutor, INService
    {
        private readonly IGoogleApiService _google;
        private readonly DbService _db;
        private readonly IEmbedBuilderService _eb;
        private readonly Bot _bot;

        private readonly ConcurrentDictionary<ulong, bool> _atcs = new();
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, (string From, string To)>> _users = new();

        public TranslateService(IGoogleApiService google,
            DbService db,
            IEmbedBuilderService eb,
            Bot bot)
        {
            _google = google;
            _db = db;
            _eb = eb;
            _bot = bot;
        }

        public async Task OnReadyAsync()
        {
            var ctx = _db.GetDbContext();

            var guilds = _bot.AllGuildConfigs.Select(x => x.GuildId).ToList();
            var cs = await ctx.AutoTranslateChannels
                .Include(x => x.Users)
                .Where(x => guilds.Contains(x.GuildId))
                .ToListAsyncEF();

            foreach (var c in cs)
            {
                _atcs[c.ChannelId] = c.AutoDelete;
                _users[c.ChannelId] = new(c.Users.ToDictionary(x => x.UserId, x => (x.Source, x.Target)));
            }
        }

        
        public async Task LateExecute(IGuild guild, IUserMessage msg)
        {
            if (msg is IUserMessage { Channel: ITextChannel tch } um)
            {
                if (!_atcs.TryGetValue(tch.Id, out var autoDelete))
                    return;

                if (!_users.TryGetValue(tch.Id, out var users)
                    || !users.TryGetValue(um.Author.Id, out var langs))
                    return;

                var output = await _google.Translate(msg.Content, langs.From, langs.To);

                if (string.IsNullOrWhiteSpace(output))
                    return;

                var embed = _eb.Create()
                    .WithOkColor();
                
                if (autoDelete)
                {
                    embed
                        .WithAuthor(um.Author.ToString(), um.Author.GetAvatarUrl())
                        .AddField(langs.From, um.Content)
                        .AddField(langs.To, output);

                    await tch.EmbedAsync(embed);
                    
                    try
                    {
                        await um.DeleteAsync();
                    }
                    catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                    {
                        _atcs.TryUpdate(tch.Id, false, true);
                    }

                    return;
                }

                await um.ReplyAsync(embed: embed
                    .AddField(langs.To, output)
                    .Build());
            }
        }

        public async Task<string> Translate(string source, string target, string text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is empty or null", nameof(text));

            var res = await _google.Translate(text, source, target).ConfigureAwait(false);
            return res.SanitizeMentions(true);
        }

        public async Task<bool> ToggleAtl(ulong guildId, ulong channelId, bool autoDelete)
        {
            var ctx = _db.GetDbContext();

            var old = await ctx.AutoTranslateChannels
                .ToLinqToDBTable()
                .FirstOrDefaultAsyncLinqToDB(x => x.ChannelId == channelId);
            
            if (old is null)
            {
                ctx.AutoTranslateChannels
                    .Add(new()
                    {
                        GuildId = guildId,
                        ChannelId = channelId,
                        AutoDelete = autoDelete,
                    });
                
                await ctx.SaveChangesAsync();

                _atcs[channelId] = autoDelete;
                _users[channelId] = new();
                
                return true;
            }

            // if autodelete value is different, update the autodelete value
            // instead of disabling
            if (old.AutoDelete != autoDelete)
            {
                old.AutoDelete = autoDelete;
                await ctx.SaveChangesAsync();
                _atcs[channelId] = autoDelete;
                return true;
            }

            await ctx.AutoTranslateChannels
                .ToLinqToDBTable()
                .DeleteAsync(x => x.ChannelId == channelId);
            
            await ctx.SaveChangesAsync();
            _atcs.TryRemove(channelId, out _);
            _users.TryRemove(channelId, out _);

            return false;
        }


        public async Task<bool?> RegisterUserAsync(ulong userId, ulong channelId, string from, string to)
        {
            var ctx = _db.GetDbContext();
            var ch = await ctx.AutoTranslateChannels
                .ToLinqToDBTable()
                .GetByChannelId(channelId);
            
            if (ch is null)
                return null;

            var user = ch.Users
                .FirstOrDefault(x => x.UserId == userId);

            if (user is null)
            {
                ch.Users.Add(user = new()
                {
                    Source = from,
                    Target = to,
                    UserId = userId,
                });
                
                await ctx.SaveChangesAsync();

                var dict = _users.GetOrAdd(channelId, new ConcurrentDictionary<ulong, (string, string)>());
                dict[userId] = (from, to);

                return true;
            }

            ctx.AutoTranslateUsers.Remove(user);
            await ctx.SaveChangesAsync();
            
            if (_users.TryGetValue(channelId, out var inner))
                inner.TryRemove(userId, out _);
            
            return true;
        }

        public async Task<bool> UnregisterUser(ulong channelId, ulong userId)
        {
            var ctx = _db.GetDbContext();
            var rows = await ctx.AutoTranslateUsers
                .ToLinqToDBTable()
                .DeleteAsync(x => x.UserId == userId &&
                                  x.Channel.ChannelId == channelId);

            if (_users.TryGetValue(channelId, out var inner))
                inner.TryRemove(userId, out _);
            
            await ctx.SaveChangesAsync();
            return rows > 0;
        }
        
        public IEnumerable<string> GetLanguages() => _google.Languages;
    } 
}