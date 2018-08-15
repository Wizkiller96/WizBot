﻿using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using SixLabors.ImageSharp.PixelFormats;

namespace WizBot.Core.Common.TypeReaders
{
    public class Rgba32TypeReader : WizBotTypeReader<Rgba32>
    {
        public Rgba32TypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            await Task.Yield();

            input = input.Replace("#", "", StringComparison.InvariantCulture);
            try
            {
                return TypeReaderResult.FromSuccess(Rgba32.FromHex(input));
            }
            catch
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Parameter is not a valid color hex.");
            }
        }
    }
}
