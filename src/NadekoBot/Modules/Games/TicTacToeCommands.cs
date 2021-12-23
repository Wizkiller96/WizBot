﻿using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public class TicTacToeCommands : NadekoSubmodule<GamesService>
    {
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1, 1);
        private readonly DiscordSocketClient _client;

        public TicTacToeCommands(DiscordSocketClient client)
        {
            _client = client;
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [NadekoOptions(typeof(TicTacToe.Options))]
        public async Task TicTacToe(params string[] args)
        {
            var (options, _) = OptionsParser.ParseFrom(new TicTacToe.Options(), args);
            var channel = (ITextChannel)ctx.Channel;

            await _sem.WaitAsync(1000).ConfigureAwait(false);
            try
            {
                if (_service.TicTacToeGames.TryGetValue(channel.Id, out var game))
                {
                    var _ = Task.Run(async () =>
                    {
                        await game.Start((IGuildUser)ctx.User).ConfigureAwait(false);
                    });
                    return;
                }
                game = new(base.Strings, this._client, channel, (IGuildUser)ctx.User, options, _eb);
                _service.TicTacToeGames.Add(channel.Id, game);
                await ReplyConfirmLocalizedAsync(strs.ttt_created).ConfigureAwait(false);

                game.OnEnded += g =>
                {
                    _service.TicTacToeGames.Remove(channel.Id);
                    _sem.Dispose();
                };
            }
            finally
            {
                _sem.Release();
            }
        }
    }
}