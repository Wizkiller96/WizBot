namespace WizBot;

/// <summary>
/// Represents essential interacation data
/// </summary>
/// <param name="Emote">Emote which will show on a button</param>
/// <param name="CustomId">Custom interaction id</param>
public record WizBotInteractionData(IEmote Emote, string CustomId);