#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NadekoBot.Services;

namespace NadekoBot.Modules.Games.Hangman
{
    public interface IHangmanSource : INService
    {
        public IReadOnlyCollection<string> GetCategories();
        public void Reload();
        public bool GetTerm(string? category, [NotNullWhen(true)] out HangmanTerm? term);
    }
}