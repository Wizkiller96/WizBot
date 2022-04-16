using Discord;
using NadekoBot;

namespace Nadeko.Snake;

/// <summary>
/// Commands which take this class as a first parameter can be executed in both DMs and Servers 
/// </summary>
public abstract class AnyContext
{
    /// <summary>
    /// Channel from the which the command is invoked
    /// </summary>
    public abstract IMessageChannel Channel { get; }
    
    /// <summary>
    /// Message which triggered the command
    /// </summary>
    public abstract IUserMessage Message { get; }
    
    /// <summary>
    /// The user who invoked the command
    /// </summary>
    public abstract IUser User { get; }

    /// <summary>
    /// Provides access to strings used by this medusa
    /// </summary>
    public abstract IMedusaStrings Strings { get; } 
    
    /// <summary>
    /// Gets a formatted localized string using a key and arguments which should be formatted in
    /// </summary>
    /// <param name="key">The key of the string as specified in localization files</param>
    /// <param name="args">Arguments (if any) to format in</param>
    /// <returns>A formatted localized string</returns>
    public abstract string GetText(string key, object[]? args = null);
    
    /// <summary>
    /// Creates a context-aware <see cref="IEmbedBuilder"/> instance
    /// (future feature for guild-based embed colors)
    /// Any code dealing with embeds should use it for future-proofness
    /// instead of manually creating embedbuilder instances
    /// </summary>
    /// <returns>A context-aware <see cref="IEmbedBuilder"/> instance </returns>
    public abstract IEmbedBuilder Embed();
}