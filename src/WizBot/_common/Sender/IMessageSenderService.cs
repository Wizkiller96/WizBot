namespace WizBot.Extensions;

public interface IMessageSenderService
{
    ResponseBuilder Response(IMessageChannel channel);
    ResponseBuilder Response(ICommandContext ctx);
    ResponseBuilder Response(IUser user);

    ResponseBuilder Response(SocketMessageComponent smc);

    WizBotEmbedBuilder CreateEmbed();
}