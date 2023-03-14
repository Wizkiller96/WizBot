#nullable disable
namespace NadekoBot.Services;

public interface ICommandStrings
{
    string Desc { get; set; }
    string[] Args { get; set; }
}