#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Hangman
{
    public interface IHangmanService
    {
        bool StartHangman(ulong channelId, string? category, [NotNullWhen(true)] out HangmanGame.State? hangmanController);
        ValueTask<bool> StopHangman(ulong channelId);
        IReadOnlyCollection<string> GetHangmanTypes();
    }
}