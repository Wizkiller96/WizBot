namespace NadekoBot.Services;

public interface ICommandHandler
{
    string GetPrefix(IGuild ctxGuild);
}