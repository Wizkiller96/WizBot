using System.Diagnostics.CodeAnalysis;

namespace NadekoBot.Extensions;

public sealed class MessageSenderService : IMessageSenderService, INService
{
    private readonly IBotStrings _bs;

    public MessageSenderService(IBotStrings bs)
    {
        _bs = bs;
    }


    public ResponseBuilder Response(IMessageChannel channel)
        => new ResponseBuilder(_bs)
            .Channel(channel);

    public ResponseBuilder Response(ICommandContext ctx)
        => new ResponseBuilder(_bs)
            .Context(ctx);

    public ResponseBuilder Response(IUser user)
        => new ResponseBuilder(_bs)
            .User(user);
    
    // todo fix interactions
    public ResponseBuilder Response(SocketMessageComponent smc)
        => new ResponseBuilder(_bs)
            .Channel(smc.Channel);
}