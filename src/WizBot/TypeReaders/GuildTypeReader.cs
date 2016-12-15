﻿using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace WizBot.TypeReaders
{
    public class GuildTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(IUserMessage context, string input)
        {
            input = input.Trim().ToLowerInvariant();
            var guilds = WizBot.Client.GetGuilds();
            var guild = guilds.FirstOrDefault(g => g.Id.ToString().Trim().ToLowerInvariant() == input) ?? //by id
                        guilds.FirstOrDefault(g => g.Name.Trim().ToLowerInvariant() == input); //by name

            if (guild != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(guild));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No guild by that name or Id found"));
        }
    }
}
