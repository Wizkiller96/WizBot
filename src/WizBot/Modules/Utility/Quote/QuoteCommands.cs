#nullable disable warnings
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Common.Yml;
using WizBot.Db.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WizBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class QuoteCommands : WizBotModule<QuoteService>
    {
        private const string PREPEND_EXPORT =
            """
            # Keys are keywords, Each key has a LIST of quotes in the following format:
            # - id: Alphanumeric id used for commands related to the quote. (Note, when using .quotesimport, a new id will be generated.)
            #   an: Author name
            #   aid: Author id
            #   txt: Quote text

            """;

        private static readonly ISerializer _exportSerializer = new SerializerBuilder()
                                                                .WithEventEmitter(args
                                                                    => new MultilineScalarFlowStyleEmitter(args))
                                                                .WithNamingConvention(
                                                                    CamelCaseNamingConvention.Instance)
                                                                .WithIndentedSequences()
                                                                .ConfigureDefaultValuesHandling(DefaultValuesHandling
                                                                    .OmitDefaults)
                                                                .DisableAliases()
                                                                .Build();

        private readonly DbService _db;
        private readonly IHttpClientFactory _http;
        private readonly IQuoteService _qs;

        public QuoteCommands(DbService db, IQuoteService qs, IHttpClientFactory http)
        {
            _db = db;
            _http = http;
            _qs = qs;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task ListQuotes(OrderType order = OrderType.Keyword)
            => ListQuotes(1, order);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task ListQuotes(int page = 1, OrderType order = OrderType.Keyword)
        {
            page -= 1;
            if (page < 0)
                return;

            var quotes = await _service.GetAllQuotesAsync(ctx.Guild.Id, page, order);

            if (quotes.Count == 0)
            {
                await Response().Error(strs.quotes_page_none).SendAsync();
                return;
            }

            var list = quotes.Select(q => $"`{new kwum(q.Id)}` {Format.Bold(q.Keyword),-20} by {q.AuthorName}")
                             .Join("\n");

            await Response()
                  .Confirm(GetText(strs.quotes_page(page + 1)), list)
                  .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuotePrint([Leftover] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            keyword = keyword.ToUpperInvariant();

            var quote = await _service.GetQuoteByKeywordAsync(ctx.Guild.Id, keyword);

            if (quote is null)
                return;

            var repCtx = new ReplacementContext(Context);

            var text = SmartText.CreateFrom(quote.Text);
            text = await repSvc.ReplaceAsync(text, repCtx);

            await Response()
                  .Text($"`{new kwum(quote.Id)}` 📣 " + text)
                  .Sanitize()
                  .SendAsync();
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteShow(kwum quoteId)
        {
            Quote? quote;
            await using (var uow = _db.GetDbContext())
            {
                quote = uow.Set<Quote>().GetById(quoteId);
                if (quote?.GuildId != ctx.Guild.Id)
                    quote = null;
            }

            if (quote is null)
            {
                await Response().Error(strs.quotes_notfound).SendAsync();
                return;
            }

            await ShowQuoteData(quote);
        }

        private WizBotInteractionBase CreateEditInteraction(kwum id, Quote found)
        {
            var modal = new ModalBuilder()
                        .WithCustomId("quote:edit_modal")
                        .WithTitle($"Edit expression {id}")
                        .AddTextInput(new TextInputBuilder()
                                      .WithLabel(GetText(strs.response))
                                      .WithValue(found.Text)
                                      .WithMinLength(1)
                                      .WithCustomId("quote:edit_modal:response")
                                      .WithStyle(TextInputStyle.Paragraph));

            var inter = _inter.Create(ctx.User.Id,
                new ButtonBuilder()
                    .WithEmote(Emoji.Parse("📝"))
                    .WithLabel("Edit")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId("test"),
                modal,
                async (sm) =>
                {
                    var msg = sm.Data.Components.FirstOrDefault()?.Value;

                    if (!string.IsNullOrWhiteSpace(msg))
                        await QuoteEdit(id, msg);
                }
            );
            return inter;
        }

        private async Task ShowQuoteData(Quote quote)
        {
            var inter = CreateEditInteraction(quote.Id, quote);
            var eb = _sender.CreateEmbed()
                            .WithOkColor()
                            .WithTitle($"{GetText(strs.quote_id($"`{new kwum(quote.Id)}"))}`")
                            .WithDescription(Format.Sanitize(quote.Text).Replace("](", "]\\(").TrimTo(4096))
                            .AddField(GetText(strs.trigger), quote.Keyword)
                            .WithFooter(
                                GetText(strs.created_by($"{quote.AuthorName} ({quote.AuthorId})")));

            if (!(quote.Text.Length > 4096))
            {
                await Response().Embed(eb).Interaction(quote.AuthorId == ctx.User.Id ? inter : null).SendAsync();
                return;
            }

            await using var textStream = await quote.Text.ToStream();

            await Response()
                  .Embed(eb)
                  .File(textStream, "quote.txt")
                  .SendAsync();
        }

        private async Task QuoteSearchInternalAsync(string? keyword, string textOrAuthor)
        {
            if (string.IsNullOrWhiteSpace(textOrAuthor))
                return;

            keyword = keyword?.ToUpperInvariant();

            var quotes = await _service.SearchQuoteKeywordTextAsync(ctx.Guild.Id, keyword, textOrAuthor);

            await Response()
                  .Paginated()
                  .Items(quotes)
                  .PageSize(1)
                  .Page((pageQuotes, _) =>
                  {
                      var quote = pageQuotes[0];

                      var text = quote.Keyword.ToLowerInvariant() + ":  " + quote.Text;

                      return _sender.CreateEmbed()
                                    .WithOkColor()
                                    .WithTitle($"{new kwum(quote.Id)} 💬 ")
                                    .WithDescription(text);
                  })
                  .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task QuoteSearch(string textOrAuthor)
            => QuoteSearchInternalAsync(null, textOrAuthor);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task QuoteSearch(string keyword, [Leftover] string textOrAuthor)
            => QuoteSearchInternalAsync(keyword, textOrAuthor);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteId(kwum quoteId)
        {
            if (quoteId < 0)
                return;

            var quote = await _service.GetQuoteByIdAsync(quoteId);

            if (quote is null || quote.GuildId != ctx.Guild.Id)
            {
                await Response().Error(strs.quotes_notfound).SendAsync();
                return;
            }

            var infoText = $"*`{new kwum(quote.Id)}` added by {quote.AuthorName.SanitizeAllMentions()}* 🗯️ "
                           + quote.Keyword.ToLowerInvariant().SanitizeAllMentions()
                           + ":\n";


            var repCtx = new ReplacementContext(Context);
            var text = SmartText.CreateFrom(quote.Text);
            text = await repSvc.ReplaceAsync(text, repCtx);
            await Response()
                  .Text(infoText + text)
                  .Sanitize()
                  .SendAsync();
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteAdd(string keyword, [Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                return;

            keyword = keyword.ToUpperInvariant();

            Quote q;
            await using (var uow = _db.GetDbContext())
            {
                uow.Set<Quote>()
                   .Add(q = new()
                   {
                       AuthorId = ctx.Message.Author.Id,
                       AuthorName = ctx.Message.Author.Username,
                       GuildId = ctx.Guild.Id,
                       Keyword = keyword,
                       Text = text
                   });
                await uow.SaveChangesAsync();
            }

            await Response().Confirm(strs.quote_added_new(Format.Code(new kwum(q.Id).ToString()))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteEdit(kwum quoteId, [Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Quote q;
            await using (var uow = _db.GetDbContext())
            {
                var intId = (int)quoteId;
                var result = await uow.GetTable<Quote>()
                                      .Where(x => x.Id == intId && x.AuthorId == ctx.User.Id)
                                      .Set(x => x.Text, text)
                                      .UpdateWithOutputAsync((del, ins) => ins);

                q = result.FirstOrDefault();

                await uow.SaveChangesAsync();
            }

            if (q is not null)
            {
                await Response()
                      .Embed(_sender.CreateEmbed()
                                    .WithOkColor()
                                    .WithTitle(GetText(strs.quote_edited))
                                    .WithDescription($"#{quoteId}")
                                    .AddField(GetText(strs.trigger), q.Keyword)
                                    .AddField(GetText(strs.response),
                                        text.Length > 1024 ? GetText(strs.redacted_too_long) : text))
                      .SendAsync();
            }
            else
            {
                await Response().Error(strs.expr_no_found_id).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteDelete(kwum quoteId)
        {
            var hasManageMessages = ((IGuildUser)ctx.Message.Author).GuildPermissions.ManageMessages;

            var success = false;
            string response;
            await using (var uow = _db.GetDbContext())
            {
                var q = uow.Set<Quote>().GetById(quoteId);

                if (q?.GuildId != ctx.Guild.Id || (!hasManageMessages && q.AuthorId != ctx.Message.Author.Id))
                    response = GetText(strs.quotes_remove_none);
                else
                {
                    uow.Set<Quote>().Remove(q);
                    await uow.SaveChangesAsync();
                    success = true;
                    response = GetText(strs.quote_deleted(new kwum(quoteId)));
                }
            }

            if (success)
                await Response().Confirm(response).SendAsync();
            else
                await Response().Error(response).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public Task QuoteDeleteAuthor(IUser user)
            => QuoteDeleteAuthor(user.Id);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task QuoteDeleteAuthor(ulong userId)
        {
            var hasManageMessages = ((IGuildUser)ctx.Message.Author).GuildPermissions.ManageMessages;

            if (userId == ctx.User.Id || hasManageMessages)
            {
                var deleted = await _qs.DeleteAllAuthorQuotesAsync(ctx.Guild.Id, userId);
                await Response().Confirm(strs.quotes_deleted_count(deleted)).SendAsync();
            }
            else
            {
                await Response().Error(strs.insuf_perms_u).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task DelAllQuotes([Leftover] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            keyword = keyword.ToUpperInvariant();

            await using (var uow = _db.GetDbContext())
            {
                await _service.RemoveAllByKeyword(ctx.Guild.Id, keyword.ToUpperInvariant());

                await uow.SaveChangesAsync();
            }

            await Response().Confirm(strs.quotes_deleted(Format.Bold(keyword.SanitizeAllMentions()))).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task QuotesExport()
        {
            var quotes = _service.GetForGuild(ctx.Guild.Id).ToList();

            var exprsDict = quotes.GroupBy(x => x.Keyword)
                                  .ToDictionary(x => x.Key, x => x.Select(ExportedQuote.FromModel));

            var text = PREPEND_EXPORT + _exportSerializer.Serialize(exprsDict).UnescapeUnicodeCodePoints();

            await using var stream = await text.ToStream();
            await ctx.Channel.SendFileAsync(stream, "quote-export.yml");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Ratelimit(300)]
#if GLOBAL_WIZBOT
            [OwnerOnly]
#endif
        public async Task QuotesImport([Leftover] string? input = null)
        {
            input = input?.Trim();

            _ = ctx.Channel.TriggerTypingAsync();

            if (input is null)
            {
                var attachment = ctx.Message.Attachments.FirstOrDefault();
                if (attachment is null)
                {
                    await Response().Error(strs.expr_import_no_input).SendAsync();
                    return;
                }

                using var client = _http.CreateClient();
                input = await client.GetStringAsync(attachment.Url);

                if (string.IsNullOrWhiteSpace(input))
                {
                    await Response().Error(strs.expr_import_no_input).SendAsync();
                    return;
                }
            }

            var succ = await ImportExprsAsync(ctx.Guild.Id, input);
            if (!succ)
            {
                await Response().Error(strs.expr_import_invalid_data).SendAsync();
                return;
            }

            await ctx.OkAsync();
        }

        private async Task<bool> ImportExprsAsync(ulong guildId, string input)
        {
            Dictionary<string, List<ExportedQuote>> data;
            try
            {
                data = Yaml.Deserializer.Deserialize<Dictionary<string, List<ExportedQuote>>>(input);
                if (data.Sum(x => x.Value.Count) == 0)
                    return false;
            }
            catch
            {
                return false;
            }

            await using var uow = _db.GetDbContext();
            foreach (var entry in data)
            {
                var keyword = entry.Key;
                await uow.Set<Quote>()
                         .AddRangeAsync(entry.Value.Where(quote => !string.IsNullOrWhiteSpace(quote.Txt))
                                             .Select(quote => new Quote
                                             {
                                                 GuildId = guildId,
                                                 Keyword = keyword,
                                                 Text = quote.Txt,
                                                 AuthorId = quote.Aid,
                                                 AuthorName = quote.An
                                             }));
            }

            await uow.SaveChangesAsync();
            return true;
        }

        public class ExportedQuote
        {
            public string Id { get; set; }
            public string An { get; set; }
            public ulong Aid { get; set; }
            public string Txt { get; set; }

            public static ExportedQuote FromModel(Quote quote)
                => new()
                {
                    Id = ((kwum)quote.Id).ToString(),
                    An = quote.AuthorName,
                    Aid = quote.AuthorId,
                    Txt = quote.Text
                };
        }
    }
}