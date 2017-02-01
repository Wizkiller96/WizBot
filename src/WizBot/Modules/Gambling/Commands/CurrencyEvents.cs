using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Services.Database;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEvents : ModuleBase
        {
            public enum CurrencyEvent
            {
                FlowerReaction,
                SneakyGameStatus
            }
            //flower reaction event
            public static readonly ConcurrentHashSet<ulong> _flowerReactionAwardedUsers = new ConcurrentHashSet<ulong>();
            public static readonly ConcurrentHashSet<ulong> _sneakyGameAwardedUsers = new ConcurrentHashSet<ulong>();


            private static readonly char[] _sneakyGameStatusChars = Enumerable.Range(48, 10)
                .Concat(Enumerable.Range(65, 26))
                .Concat(Enumerable.Range(97, 26))
                .Select(x => (char)x)
                .ToArray();

            private static string _secretCode = String.Empty;

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                var channel = (ITextChannel)Context.Channel;
                try
                {

                    switch (e)
                    {
                        case CurrencyEvent.FlowerReaction:
                            await FlowerReactionEvent(Context).ConfigureAwait(false);
                            break;
                        case CurrencyEvent.SneakyGameStatus:
                            await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                            break;
                        default:
                            break;
                    }
                }
                catch { }
            }

            public static async Task SneakyGameStatusEvent(CommandContext Context, int? arg)
            {
                int num;
                if (arg == null || arg< 5)
                    num = 60;
                else
                    num = arg.Value;

                if (_secretCode != String.Empty)
                    return;
                var rng = new WizBotRandom();

                for (int i = 0; i< 5; i++)
                {
                    _secretCode += _sneakyGameStatusChars[rng.Next(0, _sneakyGameStatusChars.Length)];
                }

                await WizBot.Client.SetGameAsync($"type {_secretCode} for " + WizBot.BotConfig.CurrencyPluralName)
                    .ConfigureAwait(false);
                try
                {
                    await Context.Channel.SendConfirmAsync($"SneakyGameStatus event started",
                        $"Users must type a secret code to get 100 currency.\n" +
                        $"Lasts {num} seconds. Don't tell anyone. Shhh.")
                        .ConfigureAwait(false);
                }
                catch { }


                WizBot.Client.MessageReceived += SneakyGameMessageReceivedEventHandler;
                await Task.Delay(num* 1000);
                WizBot.Client.MessageReceived -= SneakyGameMessageReceivedEventHandler;

                _sneakyGameAwardedUsers.Clear();
                _secretCode = String.Empty;

                await WizBot.Client.SetGameAsync($"SneakyGame event ended.")
                    .ConfigureAwait(false);
            }

            private static Task SneakyGameMessageReceivedEventHandler(SocketMessage arg)
            {
                if (arg.Content == _secretCode &&
                    _sneakyGameAwardedUsers.Add(arg.Author.Id))
                {
                    var _ = Task.Run(async () =>
                    {
                        await CurrencyHandler.AddCurrencyAsync(arg.Author, "Sneaky Game Event", 100, false)
                            .ConfigureAwait(false);

                        try { await arg.DeleteAsync(new RequestOptions() { RetryMode = RetryMode.AlwaysFail }).ConfigureAwait(false); }
                        catch { }
                    });
                }

                return Task.Delay(0);
            }

            public static async Task FlowerReactionEvent(CommandContext Context)
            {
                var msg = await Context.Channel.SendConfirmAsync("Flower reaction event started!",
                    "Add 🌸 reaction to this message to get 100" + WizBot.BotConfig.CurrencySign,
                    footer: "This event is active for 24 hours.")
                                               .ConfigureAwait(false);
                try { await msg.AddReactionAsync("🌸").ConfigureAwait(false); }
                catch
                {
                    try { await msg.AddReactionAsync("🌸").ConfigureAwait(false); }
                    catch
                    {
                        try { await msg.DeleteAsync().ConfigureAwait(false); }
                        catch { return; }
                    }
                }
                using (msg.OnReaction(async (r) =>
                {
                    try
                    {
                        if (r.Emoji.Name == "🌸" && r.User.IsSpecified && ((DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5) && _flowerReactionAwardedUsers.Add(r.User.Value.Id))
                         {
                             try { await CurrencyHandler.AddCurrencyAsync(r.User.Value, "Flower Reaction Event", 100, false).ConfigureAwait(false); } catch { }
                         }
                    }
                    catch { }
                }))
                {
                    await Task.Delay(TimeSpan.FromHours(24)).ConfigureAwait(false);
                    try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    _flowerReactionAwardedUsers.Clear();
                }
            }
        }
    }
}