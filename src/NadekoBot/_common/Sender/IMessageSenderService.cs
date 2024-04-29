namespace NadekoBot.Extensions;

public interface IMessageSenderService
{
    ResponseBuilder Response(IMessageChannel channel);
    ResponseBuilder Response(ICommandContext hannel);
}