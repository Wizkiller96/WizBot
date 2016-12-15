﻿using Discord;
using Discord.Commands;
using ImageSharp;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Modules.Gambling.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DrawCommands
        {
            private static readonly ConcurrentDictionary<IGuild, Cards> AllDecks = new ConcurrentDictionary<IGuild, Cards>();


            public DrawCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            private const string cardsPath = "data/images/cards";
            private Logger _log { get; }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Draw(IUserMessage msg, int num = 1)
            {
                var channel = (ITextChannel)msg.Channel;
                var cards = AllDecks.GetOrAdd(channel.Guild, (s) => new Cards());
                var images = new List<Image>();
                var cardObjects = new List<Cards.Card>();
                if (num > 5) num = 5;
                for (var i = 0; i < num; i++)
                {
                    if (cards.CardPool.Count == 0 && i != 0)
                    {
                        try { await channel.SendErrorAsync("No more cards in a deck.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    using (var stream = File.OpenRead(Path.Combine(cardsPath, currentCard.ToString().ToLowerInvariant()+ ".jpg").Replace(' ','_')))
                        images.Add(new Image(stream));
                }
                MemoryStream bitmapStream = new MemoryStream();
                images.Merge().SaveAsPng(bitmapStream);
                bitmapStream.Position = 0;
                //todo CARD NAMES?
                var toSend = $"{msg.Author.Mention}";
                if (cardObjects.Count == 5)
                    toSend += $" drew `{Cards.GetHandValue(cardObjects)}`";

                await channel.SendFileAsync(bitmapStream, images.Count + " cards.jpg", toSend).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShuffleDeck(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                AllDecks.AddOrUpdate(channel.Guild,
                        (g) => new Cards(),
                        (g, c) =>
                        {
                            c.Restart();
                            return c;
                        });

                await channel.SendConfirmAsync("Deck reshuffled.").ConfigureAwait(false);
            }
        }
    }
}