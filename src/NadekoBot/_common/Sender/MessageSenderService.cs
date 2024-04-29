using System.Diagnostics.CodeAnalysis;

namespace NadekoBot.Extensions;

public sealed class MessageSenderService : IMessageSenderService, INService
{
    private readonly IBotStrings _bs;
    private readonly IEmbedBuilderService _ebs;

    public MessageSenderService(IBotStrings bs, IEmbedBuilderService ebs)
    {
        _bs = bs;
        _ebs = ebs;
    }


    public ResponseBuilder Response(IMessageChannel channel)
        => new ResponseBuilder(_bs, _ebs)
            .Channel(channel);

    public ResponseBuilder Response(ICommandContext ctx)
        => new ResponseBuilder(_bs, _ebs)
            .Context(ctx);
}