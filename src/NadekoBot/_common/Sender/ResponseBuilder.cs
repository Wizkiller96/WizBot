namespace NadekoBot.Extensions;

public sealed partial class ResponseBuilder
{
    private ICommandContext? ctx = null;
    private IMessageChannel? channel = null;
    private Embed? embed = null;
    private string? plainText = null;
    private IReadOnlyCollection<EmbedBuilder>? embeds = null;
    private IUserMessage? msg = null;
    private IUser? user = null;
    private bool sanitizeMentions = true;
    private LocStr? locTxt;
    private object[] locParams = [];
    private bool shouldReply = true;
    private readonly IBotStrings _bs;
    private EmbedBuilder? embedBuilder = null;
    private NadekoInteraction? inter;
    private Stream? fileStream = null;
    private string? fileName = null;

    public ResponseBuilder(IBotStrings bs)
    {
        _bs = bs;
    }

    private MessageReference? CreateMessageReference(IMessageChannel targetChannel)
    {
        if (!shouldReply)
            return null;

        var replyTo = msg ?? ctx?.Message;
        // what message are we replying to
        if (replyTo is null)
            return null;

        // we have to have a channel where we are sending the message in order to know whether we can reply to it
        if (targetChannel.Id != replyTo.Channel.Id)
            return null;

        return new(replyTo.Id,
            replyTo.Channel.Id,
            (replyTo.Channel as ITextChannel)?.GuildId,
            failIfNotExists: false);
    }

    public ResponseMessageModel Build(bool ephemeral = false)
    {
        // todo use ephemeral in interactions
        var targetChannel = InternalResolveChannel() ?? throw new ArgumentNullException(nameof(channel));
        var msgReference = CreateMessageReference(targetChannel);

        var txt = GetText(locTxt);
        // todo check message  sanitization

        var buildModel = new ResponseMessageModel()
        {
            TargetChannel = targetChannel,
            MessageReference = msgReference,
            Text = txt,
            User = ctx?.User,
            Embed = embed ?? embedBuilder?.Build(),
            Embeds = embeds?.Map(x => x.Build()),
            SanitizeMentions = sanitizeMentions ? new(AllowedMentionTypes.Users) : AllowedMentions.All
        };

        return buildModel;
    }

    public Task<IUserMessage> SendAsync(bool ephemeral = false)
    {
        var model = Build(ephemeral);
        return SendAsync(model);
    }

    public async Task<IUserMessage> SendAsync(ResponseMessageModel model)
    {
        if (this.fileStream is Stream stream)
            return await model.TargetChannel.SendFileAsync(stream,
                filename: fileName,
                model.Text,
                embed: model.Embed,
                components: null,
                allowedMentions: model.SanitizeMentions,
                messageReference: model.MessageReference);

        return await model.TargetChannel.SendMessageAsync(
            model.Text,
            embed: model.Embed,
            embeds: model.Embeds,
            components: null,
            allowedMentions: model.SanitizeMentions,
            messageReference: model.MessageReference);
    }

    private ulong? InternalResolveGuildId(IMessageChannel? targetChannel)
        => ctx?.Guild?.Id ?? (targetChannel as ITextChannel)?.GuildId;

    // todo not good, has to go to the user
    private IMessageChannel? InternalResolveChannel()
        => channel ?? ctx?.Channel ?? msg?.Channel;

    private string? GetText(LocStr? locStr)
    {
        var targetChannel = InternalResolveChannel();
        var guildId = InternalResolveGuildId(targetChannel);
        return locStr is LocStr ls ? _bs.GetText(ls.Key, guildId, locParams) : plainText;
    }

    private string GetText(LocStr locStr)
    {
        var targetChannel = InternalResolveChannel();
        var guildId = InternalResolveGuildId(targetChannel);
        return _bs.GetText(locStr.Key, guildId, locStr.Params);
    }

    public ResponseBuilder Text(LocStr str)
    {
        locTxt = str;
        return this;
    }

    public ResponseBuilder Text(SmartText text)
    {
        if (text is SmartPlainText spt)
            plainText = spt.Text;
        else if (text is SmartEmbedText set)
            embed = set.GetEmbed().Build();
        else if (text is SmartEmbedTextArray ser)
            embeds = ser.GetEmbedBuilders();

        return this;
    }

    private ResponseBuilder InternalColoredText(string text, EmbedColor color)
    {
        embed = new EmbedBuilder()
                .WithColor(color)
                .WithDescription(text)
                .Build();

        return this;
    }

    private EmbedBuilder CreateEmbedInternal(
        string? title,
        string? text,
        string? url,
        string? footer = null)
    {
        var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription(text);

        if (!string.IsNullOrWhiteSpace(url))
            embed = embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed = embed.WithFooter(footer);

        return embed;
    }

    private EmbedBuilder PaintEmbedInternal(EmbedBuilder eb, EmbedColor color)
        => color switch
        {
            EmbedColor.Ok => eb.WithOkColor(),
            EmbedColor.Pending => eb.WithPendingColor(),
            EmbedColor.Error => eb.WithErrorColor(),
        };

    public ResponseBuilder Error(
        string? title,
        string? text,
        string? url = null,
        string? footer = null)
    {
        var eb = CreateEmbedInternal(title, text, url, footer);
        embed = PaintEmbedInternal(eb, EmbedColor.Error).Build();
        return this;
    }


    public ResponseBuilder Confirm(
        string? title,
        string? text,
        string? url = null,
        string? footer = null)
    {
        var eb = CreateEmbedInternal(title, text, url, footer);
        embed = PaintEmbedInternal(eb, EmbedColor.Error).Build();
        return this;
    }

    public ResponseBuilder Confirm(string text)
        => InternalColoredText(text, EmbedColor.Ok);

    public ResponseBuilder Confirm(LocStr str)
        => Confirm(GetText(str));

    public ResponseBuilder Pending(string text)
        => InternalColoredText(text, EmbedColor.Ok);

    public ResponseBuilder Pending(LocStr str)
        => Pending(GetText(str));

    public ResponseBuilder Error(string text)
        => InternalColoredText(text, EmbedColor.Error);

    public ResponseBuilder Error(LocStr str)
        => Error(GetText(str));


    public ResponseBuilder UserBasedMentions()
    {
        sanitizeMentions = !((InternalResolveUser() as IGuildUser)?.GuildPermissions.MentionEveryone ?? false);
        return this;
    }

    private IUser? InternalResolveUser()
        => ctx?.User ?? user ?? msg?.Author;

    // todo embed colors

    public ResponseBuilder Embed(EmbedBuilder eb)
    {
        embedBuilder = eb;
        return this;
    }

    public ResponseBuilder Channel(IMessageChannel channel)
    {
        this.channel = channel;
        return this;
    }

    public ResponseBuilder Sanitize(bool shouldSantize = true)
    {
        sanitizeMentions = shouldSantize;
        return this;
    }

    public ResponseBuilder Context(ICommandContext ctx)
    {
        this.ctx = ctx;
        return this;
    }

    public ResponseBuilder Message(IUserMessage msg)
    {
        this.msg = msg;
        return this;
    }

    public ResponseBuilder User(IUser user)
    {
        this.user = user;
        return this;
    }

    public ResponseBuilder NoReply()
    {
        shouldReply = false;
        return this;
    }

    public ResponseBuilder Interaction(NadekoInteraction? interaction)
    {
        // todo implement
        inter = interaction;
        return this;
    }

    public ResponseBuilder Embeds(IReadOnlyCollection<EmbedBuilder> inputEmbeds)
    {
        embeds = inputEmbeds;
        return this;
    }

    public ResponseBuilder FileName(Stream fileStream, string fileName)
    {
        this.fileStream = fileStream;
        this.fileName = fileName;
        return this;
    }

    public PaginatedResponseBuilder Paginated()
        => new(this);
}

public class PaginatedResponseBuilder
{
    protected readonly ResponseBuilder _builder;

    public PaginatedResponseBuilder(ResponseBuilder builder)
    {
        _builder = builder;
    }

    public SourcedPaginatedResponseBuilder<T> Items<T>(IReadOnlyCollection<T> items)
        => new SourcedPaginatedResponseBuilder<T>(_builder)
            .Items(items);
}

public sealed class SourcedPaginatedResponseBuilder<T> : PaginatedResponseBuilder
{
    private IReadOnlyCollection<T>? items;
    public Func<IReadOnlyList<T>, int, Task<EmbedBuilder>> PageFunc { get; private set; }
    public Func<int, Task<IEnumerable<T>>> ItemsFunc { get; set; }
    public int TotalElements { get; private set; } = 1;
    public int ItemsPerPage { get; private set; } = 9;
    public bool AddPaginatedFooter { get; private set; } = true;
    public bool IsEphemeral { get; private set; }

    public SourcedPaginatedResponseBuilder(ResponseBuilder builder)
        : base(builder)
    {
    }

    public SourcedPaginatedResponseBuilder<T> Items(IReadOnlyCollection<T> items)
    {
        this.items = items;
        ItemsFunc = (i) => Task.FromResult(this.items.Skip(i * ItemsPerPage).Take(ItemsPerPage));
        return this;
    }


    public SourcedPaginatedResponseBuilder<T> PageSize(int i)
    {
        ItemsPerPage = i;
        return this;
    }

    public SourcedPaginatedResponseBuilder<T> CurrentPage(int i)
    {
        InitialPage = i;
        return this;
    }

    // todo use it
    public int InitialPage { get; set; }

    public SourcedPaginatedResponseBuilder<T> Page(Func<IReadOnlyList<T>, int, EmbedBuilder> pageFunc)
    {
        this.PageFunc = (xs, x) => Task.FromResult(pageFunc(xs, x));
        return this;
    }

    public SourcedPaginatedResponseBuilder<T> Page(Func<IReadOnlyList<T>, int, Task<EmbedBuilder>> pageFunc)
    {
        this.PageFunc = pageFunc;
        return this;
    }

    public SourcedPaginatedResponseBuilder<T> AddFooter()
    {
        AddPaginatedFooter = true;
        return this;
    }

    public SourcedPaginatedResponseBuilder<T> Ephemeral()
    {
        IsEphemeral = true;
        return this;
    }


    public Task SendAsync()
    {
        var paginationSender = new ResponseBuilder.PaginationSender<T>(
            this,
            _builder);

        return paginationSender.SendAsync(IsEphemeral);
    }
}