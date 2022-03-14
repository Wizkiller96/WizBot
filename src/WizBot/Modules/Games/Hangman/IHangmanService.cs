﻿using System.Diagnostics.CodeAnalysis;

namespace WizBot.Modules.Games.Hangman;

public interface IHangmanService
{
    bool StartHangman(ulong channelId, string? category, [NotNullWhen(true)] out HangmanGame.State? hangmanController);
    ValueTask<bool> StopHangman(ulong channelId);
    IReadOnlyCollection<string> GetHangmanTypes();
}