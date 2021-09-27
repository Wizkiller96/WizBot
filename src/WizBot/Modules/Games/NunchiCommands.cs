﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common.Nunchi;
using WizBot.Modules.Games.Services;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class NunchiCommands : WizBotSubmodule<GamesService>
        {
            private readonly DiscordSocketClient _client;

            public NunchiCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Nunchi()
            {
                var newNunchi = new NunchiGame(ctx.User.Id, ctx.User.ToString());
                NunchiGame nunchi;

                //if a game was already active
                if ((nunchi = _service.NunchiGames.GetOrAdd(ctx.Guild.Id, newNunchi)) != newNunchi)
                {
                    // join it
                    if (!await nunchi.Join(ctx.User.Id, ctx.User.ToString()).ConfigureAwait(false))
                    {
                        // if you failed joining, that means game is running or just ended
                        // await ReplyErrorLocalized("nunchi_already_started").ConfigureAwait(false);
                        return;
                    }

                    await ReplyErrorLocalizedAsync(strs.nunchi_joined(nunchi.ParticipantCount));
                    return;
                }


                try { await ConfirmLocalizedAsync(strs.nunchi_created); } catch { }

                nunchi.OnGameEnded += Nunchi_OnGameEnded;
                //nunchi.OnGameStarted += Nunchi_OnGameStarted;
                nunchi.OnRoundEnded += Nunchi_OnRoundEnded;
                nunchi.OnUserGuessed += Nunchi_OnUserGuessed;
                nunchi.OnRoundStarted += Nunchi_OnRoundStarted;
                _client.MessageReceived += _client_MessageReceived;
                
                var success = await nunchi.Initialize().ConfigureAwait(false);
                if (!success)
                {
                    if (_service.NunchiGames.TryRemove(ctx.Guild.Id, out var game))
                        game.Dispose();
                    await ConfirmLocalizedAsync(strs.nunchi_failed_to_start).ConfigureAwait(false);
                }

                Task _client_MessageReceived(SocketMessage arg)
                {
                    var _ = Task.Run(async () =>
                    {
                        if (arg.Channel.Id != ctx.Channel.Id)
                            return;

                        if (!int.TryParse(arg.Content, out var number))
                            return;
                        try
                        {
                            await nunchi.Input(arg.Author.Id, arg.Author.ToString(), number).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    });
                    return Task.CompletedTask;
                }

                Task Nunchi_OnGameEnded(NunchiGame arg1, string arg2)
                {
                    if (_service.NunchiGames.TryRemove(ctx.Guild.Id, out var game))
                    {
                        _client.MessageReceived -= _client_MessageReceived;
                        game.Dispose();
                    }

                    if (arg2 is null)
                        return ConfirmLocalizedAsync(strs.nunchi_ended_no_winner);
                    else
                        return ConfirmLocalizedAsync(strs.nunchi_ended(Format.Bold(arg2)));
                }
            }

            private Task Nunchi_OnRoundStarted(NunchiGame arg, int cur)
            {
                return ConfirmLocalizedAsync(strs.nunchi_round_started(
                    Format.Bold(arg.ParticipantCount.ToString()), 
                    Format.Bold(cur.ToString())));
            }

            private Task Nunchi_OnUserGuessed(NunchiGame arg)
            {
                return ConfirmLocalizedAsync(strs.nunchi_next_number(Format.Bold(arg.CurrentNumber.ToString())));
            }

            private Task Nunchi_OnRoundEnded(NunchiGame arg1, (ulong Id, string Name)? arg2)
            {
                if(arg2.HasValue)
                    return ConfirmLocalizedAsync(strs.nunchi_round_ended(Format.Bold(arg2.Value.Name)));
                else
                    return ConfirmLocalizedAsync(strs.nunchi_round_ended_boot(
                        Format.Bold("\n" + string.Join("\n, ", arg1.Participants.Select(x => x.Name))))); // this won't work if there are too many users
            }

            private Task Nunchi_OnGameStarted(NunchiGame arg)
            {
                return ConfirmLocalizedAsync(strs.nunchi_started(Format.Bold(arg.ParticipantCount.ToString())));
            }
        }
    }
}