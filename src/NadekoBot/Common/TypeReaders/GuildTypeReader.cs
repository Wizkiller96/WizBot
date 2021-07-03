using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class GuildTypeReader : NadekoTypeReader<IGuild>
    {
        private readonly DiscordSocketClient _client;

        public GuildTypeReader(DiscordSocketClient client)
        {
            _client = client;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            input = input.Trim().ToUpperInvariant();
            var guilds = _client.Guilds;
            var guild = guilds.FirstOrDefault(g => g.Id.ToString().Trim().ToUpperInvariant() == input) ?? //by id
                        guilds.FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == input); //by name

            if (guild != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(guild));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No guild by that name or Id found"));
        }
    }
}
